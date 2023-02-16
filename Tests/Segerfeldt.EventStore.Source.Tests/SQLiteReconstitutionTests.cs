using NUnit.Framework;

using Segerfeldt.EventStore.Source.Internals;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Segerfeldt.EventStore.Source.Tests;

// ReSharper disable once InconsistentNaming
public class SQLiteReconstitutionTests
{
    private InMemoryConnection connection = null!;
    private EntityStore store = null!;

    [SetUp]
    public void Setup()
    {
        connection = new InMemoryConnection();
        var connectionPool = new SingletonConnectionPool(connection);
        store = new EntityStore(connectionPool);

        SQLite.Schema.CreateIfMissing(connection);
    }

    [Test]
    public void ReconstitutesEntities()
    {
        GivenEntity("an-entity", "a-type", 3);

        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity"), new EntityType("a-type"));

        Assert.That(entity, Is.Not.Null);
        Assert.That(entity?.Version, Is.EqualTo(EntityVersion.Of(3)));
    }

    [Test]
    public void ReturnsNullIfNoEntity()
    {
        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity"), new EntityType("a-type"));

        Assert.That(entity, Is.Null);
    }

    [Test]
    public void ReplaysEvent()
    {
        GivenEntity("an-entity", "a-type");
        GivenEvent("an-entity", "a-type", "an-event", @"{""meaning"":42}");

        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity"), new EntityType("a-type"));

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
        GivenEntity("an-entity", "a-type");
        GivenEvent("an-entity", "a-type", "first-event", version: 1);
        GivenEvent("an-entity", "a-type", "third-event", version: 3);
        GivenEvent("an-entity", "a-type", "second-event", version: 2);

        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity"), new EntityType("a-type"));

        Assert.That(entity?.ReplayedEvents, Is.Not.Null);

        var replayedEvents = entity!.ReplayedEvents!.ToList();
        Assert.That(replayedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(replayedEvents[1].Name, Is.EqualTo("second-event"));
        Assert.That(replayedEvents[2].Name, Is.EqualTo("third-event"));
    }

    [Test]
    public void CanReadHistoryOnly()
    {
        var timestampUTC = new DateTime(2021, 08, 12, 17, 22, 35, DateTimeKind.Utc);
        var timestamp = new DateTimeOffset(timestampUTC, TimeSpan.Zero);
        GivenEntity("an-entity", "a-type");
        GivenEvent("an-entity", "a-type", "first-event", "johan", timestamp);

        var history = store.GetHistory(new EntityId("an-entity"));

        Assert.That(history, Is.Not.Null);

        var replayedEvents = history!.Events.ToList();
        Assert.That(replayedEvents[0].Actor, Is.EqualTo("johan"));
        Assert.That(replayedEvents[0].Timestamp, Is.EqualTo(timestamp).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void ReadsHistoryInOrder()
    {
        GivenEntity("an-entity", "a-type");
        GivenEvent("an-entity", "a-type", "first-event", version: 1);
        GivenEvent("an-entity", "a-type", "third-event", version: 3);
        GivenEvent("an-entity", "a-type", "second-event", version: 2);

        var history = store.GetHistory(new EntityId("an-entity"));

        Assert.That(history, Is.Not.Null);

        var replayedEvents = history!.Events.ToList();
        Assert.That(replayedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(replayedEvents[1].Name, Is.EqualTo("second-event"));
        Assert.That(replayedEvents[2].Name, Is.EqualTo("third-event"));
    }

    [Test]
    public void CanVerifyExistence()
    {
        GivenEntity("an-entity", "a-type");

        Assert.That(store.ContainsEntity(new EntityId("an-entity")), Is.True);
    }

    [Test]
    public void CanDetectNonExistence()
    {
        Assert.That(store.ContainsEntity(new EntityId("an-entity")), Is.False);
    }

    [Test]
    public void ReturnsTimestampsAsUTC()
    {
        connection.Open();
        try
        {
            GivenEntity("an-entity", "a-type");
            GivenEvent("an-entity", "an-event", @"{""meaning"":42}");
        }
        finally
        {
            connection.Close();
        }

        var entity = store.Reconstitute<MyEntity>(new EntityId("an-entity"), new EntityType("a-type"));

        Assert.That(entity?.ReplayedEvents, Is.Not.Null);
        Assert.That(entity?.ReplayedEvents?.First().Timestamp - DateTimeOffset.UtcNow, Is.LessThan(TimeSpan.FromSeconds(1)));
        Assert.That(entity?.ReplayedEvents?.First().Timestamp.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    private void GivenEntity(string entityId, string entityType, int version = 1)
    {
        var command = connection.CreateCommand(
            "INSERT INTO Entities (id, type, version) VALUES (@entityId, @entityType, @version)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@entityType", entityType);
        command.AddParameter("@version", version);
        command.ExecuteNonQuery();
    }

    private void GivenEvent(string entityId, string entityType, string eventName, string actor, DateTimeOffset timestamp)
    {
        string commandText = @"INSERT INTO Events (entity_id, entity_type, name, details, actor, timestamp, version, position)
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
            @"INSERT INTO Events (entity_id, entity_type, name, details, actor, version, position)
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
