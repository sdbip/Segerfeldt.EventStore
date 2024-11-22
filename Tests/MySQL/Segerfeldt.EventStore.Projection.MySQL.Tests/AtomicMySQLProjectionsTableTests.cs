using Microsoft.Extensions.DependencyInjection;

using MySql.Data.MySqlClient;

namespace Segerfeldt.EventStore.Projection.MySQL.Tests;

// ReSharper disable once InconsistentNaming
public sealed class AtomicMySQLProjectionsTableTests
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("MYSQL_TEST_CONNECTION_STRING");

    private AtomicMySQLProjectionsTable table = null!;
    private TargetDatabase database = null!;

    [SetUp]
    public void SetUp()
    {
        Assert.That(connectionString, Is.Not.Null,
            "MYSQL_TEST_CONNECTION_STRING not set. Add to .runsettings file in solution root.");

        var connection = CreateConnection();
        AtomicMySQLProjectionsTable.AddSchema(connection);
        database = new TargetDatabase(CreateConnection);
        table = new AtomicMySQLProjectionsTable("source", new MockServiceProvider(database));
    }

    [TearDown]
    public void TearDown()
    {
        ClearTable();
    }

    private void ClearTable()
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand("DELETE FROM Projections");
        connection.Open();
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }
    }

    [Test]
    public void GetLastFinishedPosition_NoProjectionDone_ReturnsNull()
    {
        Assert.That(table.GetLastFinishedPosition(), Is.Null);
    }

    [Test]
    public void GetLastFinishedPosition_ProjectionDoneEarlier_ReturnsPosition()
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand("INSERT INTO Projections VALUES ('source', 42)");
        connection.Open();
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }

        Assert.That(table.GetLastFinishedPosition(), Is.EqualTo(42));
    }

    [Test]
    public void GetLastFinishedPosition_FirstProjectionDone_ReturnsPosition()
    {
        table.ProjectingPosition(42, () => { });
        Assert.That(table.GetLastFinishedPosition(), Is.EqualTo(42));
    }

    [Test]
    public void GetLastFinishedPosition_ProjectionDone_ReturnsPosition()
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand("INSERT INTO Projections VALUES ('source', 41)");
        connection.Open();
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }

        table.ProjectingPosition(42, () => { });
        Assert.That(table.GetLastFinishedPosition(), Is.EqualTo(42));
    }

    [Test]
    public void GetLastFinishedPosition_ProjectionFailed_ReturnsPreviousPosition()
    {
        table.ProjectingPosition(42, () => { throw new Exception(); });
        Assert.That(table.GetLastFinishedPosition(), Is.Null);
    }

    [Test]
    public void Projecting_OnException_RollsBackChanges()
    {
        table.ProjectingPosition(42, () =>
        {
            using var command = database.SharedOpenConnection.CreateCommand("INSERT INTO Projections VALUES ('x', 41)");
            command.ExecuteNonQuery();
            throw new Exception();
        });

        using var connection = CreateConnection();
        using var command = connection.CreateCommand("SELECT COUNT(*) FROM Projections");
        connection.Open();
        try { Assert.That(command.ExecuteScalar(), Is.EqualTo(0)); }
        finally { connection.Close(); }
    }

    private MySqlConnection CreateConnection() => new(connectionString);

    private class MockServiceProvider(TargetDatabase targetDatabase) : IKeyedServiceProvider
    {
        public object? GetKeyedService(Type serviceType, object? serviceKey) => targetDatabase;
        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) => targetDatabase;
        public object? GetService(Type serviceType) => targetDatabase;
    }
}
