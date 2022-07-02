using Npgsql;

using NUnit.Framework;

using Segerfeldt.EventStore.Source.Internals;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Segerfeldt.EventStore.Source.Tests;

public class PostgreSQLReconstitutionTests
{
    private NpgsqlConnection connection = null!;
    private EntityStore store = null!;

    [SetUp]
    public void Setup()
    {
        connection = new NpgsqlConnection("Server=localhost;Database=es_test;");
        var connectionPool = new SingletonConnectionPool(connection);
        store = new EntityStore(connectionPool);

        PostgreSQL.Schema.CreateIfMissing(connection);
    }

    [TearDown]
    public void TearDown()
    {
        connection.Open();
        connection.CreateCommand("DELETE FROM Events; DELETE FROM Entities;").ExecuteNonQuery();
        connection.Close();
    }

    [Test]
    public void ReconstitutesEntities()
    {
        connection.Open();
        try
        {
            GivenEntity("an-entity-1", "a-type", 3);
        }
        finally
        {
            connection.Close();
        }

        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity-1"), new EntityType("a-type"));

        Assert.That(entity, Is.Not.Null);
        Assert.That(entity?.Version, Is.EqualTo(EntityVersion.Of(3)));
    }

    [Test]
    public void ReturnsNullIfNoEntity()
    {
        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity-2"), new EntityType("a-type"));

        Assert.That(entity, Is.Null);
    }

    [Test]
    public void ReplaysEvent()
    {
        connection.Open();
        try
        {
            GivenEntity("an-entity-3", "a-type");
            GivenEvent("an-entity-3", "a-type", "an-event", @"{""meaning"":42}");
        }
        finally
        {
            connection.Close();
        }

        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity-3"), new EntityType("a-type"));

        Assert.That(entity?.ReplayedEvents, Is.Not.Null);
        Assert.That(entity?.ReplayedEvents?.Select(e => new
            {
                e.Name,
                e.Details
            }),
            Is.EquivalentTo(new[] { new
            {
                Name = "an-event",
                Details = @"{""meaning"":42}"
            } }));
    }

    [Test]
    public void ReplaysMultipleEventsInOrder()
    {
        connection.Open();
        try
        {
            GivenEntity("an-entity-4", "a-type");
            GivenEvent("an-entity-4", "a-type", "first-event", version: 1);
            GivenEvent("an-entity-4", "a-type", "third-event", version: 3);
            GivenEvent("an-entity-4", "a-type", "second-event", version: 2);
        }
        finally
        {
            connection.Close();
        }

        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity-4"), new EntityType("a-type"));

        Assert.That(entity?.ReplayedEvents, Is.Not.Null);

        var replayedEvents = entity!.ReplayedEvents!.ToList();
        Assert.That(replayedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(replayedEvents[1].Name, Is.EqualTo("second-event"));
        Assert.That(replayedEvents[2].Name, Is.EqualTo("third-event"));
    }

    [Test]
    public void CanReadHistoryOnly()
    {
        var timestamp = new DateTimeOffset(2021, 08, 12, 17, 22, 35, TimeSpan.Zero);
        connection.Open();
        try
        {
            GivenEntity("an-entity-5", "a-type");
            GivenEvent("an-entity-5", "a-type", "first-event", "johan", timestamp);
        }
        finally
        {
            connection.Close();
        }

        var history = store.GetHistory(new EntityId("an-entity-5"));

        Assert.That(history, Is.Not.Null);

        var replayedEvents = history!.Events.ToList();
        Assert.That(replayedEvents[0].Actor, Is.EqualTo("johan"));
        Assert.That(replayedEvents[0].Timestamp, Is.EqualTo(timestamp).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void ReadsHistoryInOrder()
    {
        connection.Open();
        try
        {
            GivenEntity("an-entity-6", "a-type");
            GivenEvent("an-entity-6", "a-type", "first-event", version: 1);
            GivenEvent("an-entity-6", "a-type", "third-event", version: 3);
            GivenEvent("an-entity-6", "a-type", "second-event", version: 2);
        }
        finally
        {
            connection.Close();
        }

        var history = store.GetHistory(new EntityId("an-entity-6"));

        Assert.That(history, Is.Not.Null);

        var replayedEvents = history!.Events.ToList();
        Assert.That(replayedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(replayedEvents[1].Name, Is.EqualTo("second-event"));
        Assert.That(replayedEvents[2].Name, Is.EqualTo("third-event"));
    }

    [Test]
    public void ReturnsTimestampsAsUTC()
    {
        connection.Open();
        try
        {
            GivenEntity("an-entity-7", "a-type");
            GivenEvent("an-entity-7", "a-type", "an-event", @"{""meaning"":42}");
        }
        finally
        {
            connection.Close();
        }

        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity-7"), new EntityType("a-type"));

        Assert.That(entity?.ReplayedEvents, Is.Not.Null);
        Assert.That(entity?.ReplayedEvents?.First().Timestamp.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(entity?.ReplayedEvents?.First().Timestamp, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
    }

    private void GivenEntity(string entityId, string entityType, int version = 1)
    {
        var command = connection.CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@entityId, @entityType, @version)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@entityType", entityType);
        command.AddParameter("@version", version);
        command.ExecuteNonQuery();
    }

    private void GivenEvent(string entityId, string entityType, string eventName, string actor, DateTimeOffset timestamp)
    {
        string commandText =
            @"INSERT INTO Events (entityId, entityType, name, details, actor, timestamp, version, position)
                    VALUES (@entityId, @entityType, @eventName, '{}', @actor, @timestamp, 1, 1)";
        var command = connection.CreateCommand(commandText);
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@entityType", entityType);
        command.AddParameter("@eventName", eventName);
        command.AddParameter("@actor", actor);
        command.AddParameter("@timestamp", timestamp.UtcDateTime.DaysSinceEpoch());
        command.ExecuteNonQuery();
    }

    private void GivenEvent(string entityId, string entityType, string eventName, string details = "{}", int version = 1)
    {
        var command = connection.CreateCommand(
            @"INSERT INTO Events (entityId, entityType, name, details, actor, version, position)
                    VALUES (@entityId, @entityType, @eventName, @details, 'test', @version, 1)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@entityType", entityType);
        command.AddParameter("@eventName", eventName);
        command.AddParameter("@details", details);
        command.AddParameter("@version", version);
        command.ExecuteNonQuery();
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class MyEntity : IEntity
    {
        public EntityId Id { get; }
        public EntityVersion Version { get; }
        public EntityType Type => new("MyEntity");
        public IEnumerable<UnpublishedEvent> UnpublishedEvents => ImmutableList<UnpublishedEvent>.Empty;

        public IEnumerable<PublishedEvent>? ReplayedEvents { get; private set; }

        public MyEntity(EntityId id, EntityVersion version)
        {
            Id = id;
            Version = version;
        }

        public void ReplayEvents(IEnumerable<PublishedEvent> events)
        {
            ReplayedEvents = events;
        }
    }
}
