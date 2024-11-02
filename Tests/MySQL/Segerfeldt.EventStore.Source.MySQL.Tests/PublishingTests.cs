using MySql.Data.MySqlClient;

namespace Segerfeldt.EventStore.Source.MySQL.Tests;

public sealed class PublishingTests
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("MYSQL_TEST_CONNECTION_STRING");

    private MySqlConnection connection = null!;
    private EventPublisher publisher = null!;

    [SetUp]
    public void Setup()
    {
        Assert.That(connectionString, Is.Not.Null,
            "MYSQL_TEST_CONNECTION_STRING not set. Add to .runsettings file in solution root.");

        connection = new MySqlConnection(connectionString);
        publisher = new EventPublisher(connection);
        Schema.CreateIfMissing(connection);
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
        publisher.Publish(
            EntityId.Value("an-entity"),
            EntityType.Name("a-type"),
            new UnpublishedEvent("an-event", new { Meaning = 42 }), "johan");

        connection.Open();
        using var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
        reader.Read();

        Assert.That(new
        {
            Entity = reader["entity_id"],
            Name = reader["name"],
            Details = reader["details"],
            Ordinal = reader["ordinal"],
            Position = reader["position"]
        }, Is.EqualTo(new
        {
            Entity = (object)"an-entity",
            Name = (object)"an-event",
            Details = (object)@"{""meaning"":42}",
            Ordinal = (object)0,
            Position = (object)0L
        }));
        connection.Close();
    }

    [Test]
    public void CanPublishNewEntity()
    {
        GivenEntity("an-entity", version: EntityVersion.Of(3));

        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(EntityId.Value("an-entity"));
        entity.Setup(e => e.Type).Returns(EntityType.Name("a-type"));
        entity.Setup(e => e.Version).Returns(EntityVersion.Of(3));
        entity.Setup(e => e.UnpublishedEvents).Returns([new UnpublishedEvent("an-event", new { Meaning = 42 })]);
        publisher.PublishChanges(entity.Object, "johan");

        connection.Open();
        using var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
        reader.Read();

        Assert.That(new
        {
            Entity = reader["entity_id"],
            Name = reader["name"],
            Details = reader["details"],
            Ordinal = reader["ordinal"],
            Position = reader["position"]
        }, Is.EqualTo(new
        {
            Entity = (object)"an-entity",
            Name = (object)"an-event",
            Details = (object)@"{""meaning"":42}",
            Ordinal = (object)0,
            Position = (object)0L
        }));
        connection.Close();
    }

    [Test]
    public void CanPublishChanges()
    {
        GivenEntity("an-entity", version: EntityVersion.Zero);

        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(EntityId.Value("an-entity"));
        entity.Setup(e => e.Type).Returns(EntityType.Name("a-type"));
        entity.Setup(e => e.Version).Returns(EntityVersion.Zero);
        entity.Setup(e => e.UnpublishedEvents).Returns([new UnpublishedEvent("an-event", new { Meaning = 42 })]);

        publisher.PublishChanges(entity.Object, "johan");

        connection.Open();
        using var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
        reader.Read();

        Assert.That(new
        {
            Entity = reader["entity_id"],
            Name = reader["name"],
            Details = reader["details"],
            Ordinal = reader["ordinal"],
            Position = reader["position"]
        }, Is.EqualTo(new
        {
            Entity = (object)"an-entity",
            Name = (object)"an-event",
            Details = (object)@"{""meaning"":42}",
            Ordinal = (object)0,
            Position = (object)0L
        }));
        connection.Close();
    }

    [Test]
    public void CannotPublishChangesIfRemoteUpdated()
    {
        GivenEntity("an-entity", version: EntityVersion.Of(3));

        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(EntityId.Value("an-entity"));
        entity.Setup(e => e.Version).Returns(EntityVersion.Of(2));
        entity.Setup(e => e.UnpublishedEvents).Returns([new UnpublishedEvent("an-event", new { })]);

        Assert.That(async () => await publisher.PublishChangesAsync(entity.Object, "johan"), Throws.Exception);
    }

    [Test]
    public void CommitsChanges()
    {
        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(EntityId.Value("an-entity"));
        entity.Setup(e => e.Type).Returns(EntityType.Name("a-type"));
        entity.Setup(e => e.Version).Returns(EntityVersion.New);
        entity.Setup(e => e.UnpublishedEvents).Returns([new UnpublishedEvent("an-event", new { })]);

        connection.Open();
        try
        {
            var transaction = connection.BeginTransaction();

            publisher.PublishChanges([entity.Object], "johan", transaction);
            transaction.Commit();

            using var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
            Assert.That(reader.Read(), Is.True);
        }
        finally { connection.Close(); }
    }

    [Test]
    public void RollsBackChanges()
    {
        var entity = new Mock<IEntity>();
        entity.Setup(e => e.Id).Returns(EntityId.Value("an-entity"));
        entity.Setup(e => e.Type).Returns(EntityType.Name("a-type"));
        entity.Setup(e => e.Version).Returns(EntityVersion.New);
        entity.Setup(e => e.UnpublishedEvents).Returns([new UnpublishedEvent("an-event", new { })]);

        connection.Open();
        try
        {
            var transaction = connection.BeginTransaction();

            publisher.PublishChanges([entity.Object], "johan", transaction);
            transaction.Rollback();

            using var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
            Assert.That(reader.Read(), Is.False);
        }
        finally { connection.Close(); }
    }

    private void GivenEntity(string id, EntityVersion version)
    {
        connection.Open();
        try
        {
            using var command = connection.CreateCommand(
                """
                INSERT INTO Entities (id, type, version)
                VALUES (@entityId, 'a-type', @version)
                """);
            command.AddParameter("@entityId", id);
            command.AddParameter("@version", version.Value);
            command.ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }
    }
}
