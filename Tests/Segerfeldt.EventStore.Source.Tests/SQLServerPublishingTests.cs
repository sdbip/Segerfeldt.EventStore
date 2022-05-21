using Moq;

using NUnit.Framework;

using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Source.Tests;

public class SQLServerPublishingTests
{
    private SqlConnection connection = null!;
    private EventPublisher publisher = null!;

    [SetUp]
    public void Setup()
    {
        connection = new SqlConnection("Server=localhost;Database=test;User Id=sa;Password=S_12345678;");
        var connectionPool = new SingletonConnectionPool(connection);
        publisher = new EventPublisher(connectionPool);
        SQLServer.Schema.CreateIfMissing(connection);
    }

    [TearDown]
    public void TearDown()
    {
        connection.Open();
        try
        {
            connection.CreateCommand("DELETE FROM Events; DELETE FROM Entities;").ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }
    }

    [Test]
    public void CanPublishSingleEvent()
    {
        publisher.Publish(new EntityId("an-entity-1"), new EntityType("a-type"), new UnpublishedEvent("an-event", new{Meaning = 42}), "johan");

        connection.Open();
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
            Entity = (object) "an-entity-1",
            Type = (object) "a-type",
            Name = (object) "an-event",
            Details = (object) @"{""meaning"":42}",
            Version = (object) 0,
            Position = (object) 0L
        }));
        connection.Close();
    }

    [Test]
    public void CanPublishChanges()
    {
        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(new EntityId("an-entity-2"));
        entity.Setup(e => e.Type).Returns(new EntityType("a-type"));
        entity.Setup(e => e.Version).Returns(EntityVersion.New);
        entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{Meaning = 42})});
        publisher.PublishChanges(entity.Object, "johan");

        connection.Open();
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
            Entity = (object) "an-entity-2",
            Type = (object) "a-type",
            Name = (object) "an-event",
            Details = (object) @"{""meaning"":42}",
            Version = (object) 0,
            Position = (object) 0L
        }));
        connection.Close();
    }

    [Test]
    public void CannotPublishChangesIfRemoteUpdated()
    {
        GivenEntity();

        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(new EntityId("an-entity-3"));
        entity.Setup(e => e.Version).Returns(EntityVersion.Of(2));
        entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{})});

        Assert.That(async () => await publisher.PublishChangesAsync(entity.Object, "johan"), Throws.Exception);
    }

    private void GivenEntity()
    {
        connection.Open();
        try
        {
            connection
                .CreateCommand("INSERT INTO Entities (id, type, version) VALUES ('an-entity-3', 'a-type', 3)")
                .ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }
    }
}
