using Segerfeldt.EventStore.Source.CommandAPI;
using Segerfeldt.EventStore.Source.Internals;

using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Source.MSSQL.Tests;

public sealed class EventStoreRepositoryTests
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("MSSQL_TEST_CONNECTION_STRING");

    private SqlConnection connection = null!;
    private EntityStoreRepository repository = null!;

    [SetUp]
    public void Setup()
    {
        Assert.That(connectionString, Is.Not.Null,
            "MSSQL_TEST_CONNECTION_STRING not set. Add to .runsettings file in solution root.");

        connection = new SqlConnection(connectionString);
        repository = new EntityStoreRepository(EventStoreConnectionFactory.Singleton(connection));

        Schema.CreateIfMissing(connection);
        connection.Open();
        connection.CreateCommand("DELETE FROM Events; DELETE FROM Entities;").ExecuteNonQuery();
        connection.Close();
    }

    [TearDown]
    public void TearDown()
    {
        connection.Open();
        connection.CreateCommand("DELETE FROM Events; DELETE FROM Entities;").ExecuteNonQuery();
        connection.Close();
    }

    [Test]
    public async Task ReconstitutesEntities()
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

        var history = await repository.GetHistoryAsync(EntityId.Value("an-entity-1"));

        Assert.Multiple(() =>
        {
            Assert.That(history.HasValue, Is.True);
            Assert.That(history?.Type, Is.EqualTo("a-type"));
            Assert.That(history?.Version, Is.EqualTo(3));
        });

    }

    [Test]
    public async Task ReturnsNullIfNoEntity()
    {
        var history = await repository.GetHistoryAsync(EntityId.Value("an-entity-2"));

        Assert.That(history, Is.Null);
    }

    [Test]
    public async Task ReadsHistoryInOrder()
    {
        connection.Open();
        try
        {
            GivenEntity("an-entity-6", "a-type");
            GivenEvent("an-entity-6", "first-event", ordinal: 1);
            GivenEvent("an-entity-6", "third-event", ordinal: 3);
            GivenEvent("an-entity-6", "second-event", ordinal: 2);
        }
        finally
        {
            connection.Close();
        }

        var history = await repository.GetHistoryAsync(EntityId.Value("an-entity-6"));
        var replayedEvents = history?.Events.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(history.HasValue, Is.True);
            Assert.That(replayedEvents?[0].Name, Is.EqualTo("first-event"));
            Assert.That(replayedEvents?[1].Name, Is.EqualTo("second-event"));
            Assert.That(replayedEvents?[2].Name, Is.EqualTo("third-event"));
        });
    }

    private void GivenEntity(string entityId, string entityType, int version = 1)
    {
        var command = connection.CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@entityId, @entityType, @version)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@entityType", entityType);
        command.AddParameter("@version", version);
        command.ExecuteNonQuery();
    }

    private void GivenEvent(string entityId, string eventName, string details = "{}", int ordinal = 1)
    {
        var command = connection.CreateCommand(
            @"INSERT INTO Events (entity_id, name, details, actor, ordinal, position)
                    VALUES (@entityId, @eventName, @details, 'test', @ordinal, 1)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@eventName", eventName);
        command.AddParameter("@details", details);
        command.AddParameter("@ordinal", ordinal);
        command.ExecuteNonQuery();
    }
}
