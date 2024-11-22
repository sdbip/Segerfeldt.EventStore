using Microsoft.Extensions.DependencyInjection;

namespace Segerfeldt.EventStore.Projection.SQLite.Tests;

// ReSharper disable once InconsistentNaming
public sealed class AtomicSQLiteProjectionsTableTests
{
    private AtomicSQLiteProjectionsTable table = null!;
    private InMemoryConnection connection = null!;

    [SetUp]
    public void SetUp()
    {
        connection = new InMemoryConnection();
        AtomicSQLiteProjectionsTable.AddSchema(connection);
        table = new AtomicSQLiteProjectionsTable("source", new MockServiceProvider(new TargetDatabase(() => connection)));
    }

    [TearDown]
    public void TearDown()
    {
        connection.Dispose();
    }

    [Test]
    public void GetLastFinishedPosition_NoProjectionDone_ReturnsNull()
    {
        Assert.That(table.GetLastFinishedPosition(), Is.Null);
    }

    [Test]
    public void GetLastFinishedPosition_ProjectionDoneEarlier_ReturnsPosition()
    {
        using var command = connection.CreateCommand("INSERT INTO Projections VALUES ('source', 42)");
        command.ExecuteNonQuery();

        Assert.That(table.GetLastFinishedPosition(), Is.EqualTo(42));
    }

    [Test]
    public void GetLastFinishedPosition_FirstProjectionDone_ReturnsPosition()
    {
        table.ProjectingPosition(42, () => {});
        Assert.That(table.GetLastFinishedPosition(), Is.EqualTo(42));
    }

    [Test]
    public void GetLastFinishedPosition_ProjectionDone_ReturnsPosition()
    {
        using var command = connection.CreateCommand("INSERT INTO Projections VALUES ('source', 41)");
        command.ExecuteNonQuery();

        table.ProjectingPosition(42, () => {});
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
            using var command = connection.CreateCommand("INSERT INTO Projections VALUES ('x', 41)");
            command.ExecuteNonQuery();
            throw new Exception();
        });

        using var command = connection.CreateCommand("SELECT COUNT(*) FROM Projections");
        Assert.That(command.ExecuteScalar(), Is.EqualTo(0));
    }

    private class MockServiceProvider(TargetDatabase targetDatabase) : IKeyedServiceProvider
    {
        public object? GetKeyedService(Type serviceType, object? serviceKey) => targetDatabase;
        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) => targetDatabase;
        public object? GetService(Type serviceType) => targetDatabase;
    }
}
