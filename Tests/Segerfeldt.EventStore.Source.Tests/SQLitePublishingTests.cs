using Moq;

using NUnit.Framework;

using System;

namespace Segerfeldt.EventStore.Source.Tests;

// ReSharper disable once InconsistentNaming
public class SQLitePublishingTests
{
    private InMemoryConnection connection = null!;
    private EventPublisher publisher = null!;

    [SetUp]
    public void Setup()
    {
        connection = new InMemoryConnection();
        var connectionPool = new SingletonConnectionPool(connection);
        publisher = new EventPublisher(connectionPool);

        SQLite.Schema.CreateIfMissing(connection);
    }

    [Test]
    public void DoesNotCrashIfSchemaExists()
    {
        SQLite.Schema.CreateIfMissing(connection);
    }

    [Test]
    public void CanPublishSingleEvent()
    {
        connection.Open();
        connection.CreateCommand("UPDATE Properties SET value = 0 WHERE name = 'next_position'").ExecuteNonQuery();
        connection.Close();

        publisher.Publish(new EntityId("an-entity"), new EntityType("a-type"), new UnpublishedEvent("an-event", new{Meaning = 42}), "johan");

        using var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
        reader.Read();

        Assert.That(new
        {
            Entity = reader["entityId"],
            Type = reader["entityType"],
            Name = reader["name"],
            Details = reader["details"],
            Version = reader["version"],
            Position = reader["position"]
        }, Is.EqualTo(new
        {
            Entity = (object) "an-entity",
            Type = (object) "a-type",
            Name = (object) "an-event",
            Details = (object) @"{""meaning"":42}",
            Version = (object) 0,
            Position = (object) 0L
        }));
    }

    [Test]
    public void CanPublishChanges()
    {
        connection.Open();
        connection.CreateCommand("UPDATE Properties SET value = 0 WHERE name = 'next_position'").ExecuteNonQuery();
        connection.Close();

        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(new EntityId("an-entity"));
        entity.Setup(e => e.Type).Returns(new EntityType("a-type"));
        entity.Setup(e => e.Version).Returns(EntityVersion.New);
        entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{Meaning = 42})});
        publisher.PublishChanges(entity.Object, "johan");

        using var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
        reader.Read();

        Assert.That(new
        {
            Entity = reader["entityId"],
            Type = reader["entityType"],
            Name = reader["name"],
            Details = reader["details"],
            Version = reader["version"],
            Position = reader["position"]
        }, Is.EqualTo(new
        {
            Entity = (object) "an-entity",
            Type = (object) "a-type",
            Name = (object) "an-event",
            Details = (object) @"{""meaning"":42}",
            Version = (object) 0,
            Position = (object) 0L
        }));
    }

    [Test]
    public void WillNotPublishChangesIfThereAreNoEvents()
    {
        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(new EntityId("an-entity"));
        entity.Setup(e => e.Type).Returns(new EntityType("a-type"));
        entity.Setup(e => e.Version).Returns(EntityVersion.New);
        entity.Setup(e => e.UnpublishedEvents).Returns(Array.Empty<UnpublishedEvent>());
        publisher.PublishChanges(entity.Object, "johan");

        connection.Open();
        var count = connection.CreateCommand("SELECT COUNT(*) FROM Events").ExecuteScalar();
        connection.Close();

        Assert.That(count, Is.Zero);
    }

    [Test]
    public void CannotPublishChangesIfRemoteUpdated()
    {
        connection
            .CreateCommand("INSERT INTO Entities (id, version) VALUES ('an-entity', 3)")
            .ExecuteNonQuery();

        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(new EntityId("an-entity"));
        entity.Setup(e => e.Version).Returns(EntityVersion.Of(2));
        entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{})});

        Assert.That(() => publisher.PublishChanges(entity.Object, "johan"), Throws.Exception);
    }
}
