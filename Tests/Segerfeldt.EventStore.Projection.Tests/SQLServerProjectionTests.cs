using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.Tests;

// ReSharper disable once InconsistentNaming
public class SQLServerProjectionTests
{
    private readonly string? connectionString = Environment.GetEnvironmentVariable("MSSQL_TEST_CONNECTION_STRING");

    private SqlConnection connection = null!;
    private EventSource eventSource = null!;
    private Mock<IPollingStrategy> delayConfiguration = null!;
    private Mock<IProjectionTracker> positionTracker = null!;

    [SetUp]
    public void Setup()
    {
        Assert.That(connectionString, Is.Not.Null,
            "MSSQL_TEST_CONNECTION_STRING not set. Add to .runsettings file in solution root.");

        connection = new SqlConnection(connectionString);
        delayConfiguration = new Mock<IPollingStrategy>();
        positionTracker = new Mock<IProjectionTracker>();

        var connectionPool = new Mock<IConnectionPool>();
        connectionPool.Setup(pool => pool.CreateConnection()).Returns(new SqlConnection("Server=localhost;Database=test;User Id=sa;Password=S_12345678;"));
        eventSource = new EventSource(new DefaultEventSourceRepository(connectionPool.Object), positionTracker.Object, delayConfiguration.Object);

        delayConfiguration
            .Setup(c => c.NextDelay(It.IsAny<int>()))
            .Returns(Timeout.Infinite);

        Source.SQLServer.Schema.CreateIfMissing(connection);
        ClearTables();
    }

    [TearDown]
    public void TearDown()
    {
        ClearTables();
    }

    [Test]
    public void ReportsEventsWithEntityIdAndDetails()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", @"{""value"":42}");

        var notifiedEvents = CaptureNotifiedEvents("first-event");

        eventSource.BeginProjecting();
        Task.Yield();

        Assert.That(notifiedEvents, Is.Not.Empty);
        Assert.That(notifiedEvents[0].EntityId, Is.EqualTo("an-entity"));
        Assert.That(notifiedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(notifiedEvents[0].Details, Is.EqualTo(@"{""value"":42}"));
    }

    [Test]
    public void ReportsEventsOrderedByVersion()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", ordinal: 1);
        GivenEvent("an-entity", "third-event", ordinal: 3);
        GivenEvent("an-entity", "second-event", ordinal: 2);

        var notifiedEvents = CaptureNotifiedEvents("first-event", "second-event", "third-event");

        eventSource.BeginProjecting();

        Assert.That(notifiedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "first-event", "second-event", "third-event" }));
        Assert.That(notifiedEvents.Select(e => e.Name), Is.EqualTo(new[] { "first-event", "second-event", "third-event" }));
    }

    [Test]
    public void NotifiesNewEvents()
    {
        var delay = new[] { 0 };
        delayConfiguration.Setup(c => c.NextDelay(It.IsAny<int>())).Returns(() => delay[0]);

        var notifiedEvents = CaptureNotifiedEvents("an-event");
        GivenEntity("an-entity");

        GivenEvent("an-entity", "an-event", ordinal: 1, position: 1);

        eventSource.BeginProjecting();
        notifiedEvents.Clear();

        GivenEvent("an-entity", "an-event", ordinal: 2, position: 2);

        Thread.Sleep(100);

        Assert.That(notifiedEvents.Select(e => e.Position), Is.EquivalentTo(new[] { 2L }));
    }

    [Test]
    public void AllowsSettingStartPosition()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", position: 32);
        GivenEvent("an-entity", "second-event", position: 33);
        positionTracker.Setup(t => t.GetLastFinishedPosition()).Returns(32);

        var notifiedEvents = CaptureNotifiedEvents("first-event", "second-event");

        eventSource.BeginProjecting();
        Task.Yield();

        Assert.That(notifiedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "second-event" }));
    }

    [Test]
    public void ReportsNewPosition()
    {
        var startingPosition = CaptureStartingPosition();
        var finishedPosition = CaptureFinishedPosition();

        GivenEntity("an-entity");
        GivenEvent("an-entity", "an-event", position: 1);

        eventSource.BeginProjecting();
        Task.Yield();

        Assert.That(startingPosition.Value, Is.EqualTo(1));
        Assert.That(finishedPosition.Value, Is.EqualTo(1));
    }

    private void GivenEntity(string entityId, int version = 1)
    {
        connection.Open();
        try
        {
            using var command = connection
                .CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@entityId, 'a-type', @version)");
            command.AddParameter("@entityId", entityId);
            command.AddParameter("@version", version);
            command.ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }
    }

    private void GivenEvent(string entityId, string eventName, string details = "{}", int ordinal = 1, long position = 1)
    {
        connection.Open();
        try
        {
            using var command = connection.CreateCommand(
                @"INSERT INTO Events (entity_id, name, details, actor, ordinal, position)
                    VALUES (@entityId, @eventName, @details, 'test', @ordinal, @position)");
            command.AddParameter("@entityId", entityId);
            command.AddParameter("@eventName", eventName);
            command.AddParameter("@details", details);
            command.AddParameter("@ordinal", ordinal);
            command.AddParameter("@position", position);
            command.ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }
    }

    private List<Event> CaptureNotifiedEvents(params string[] eventNames)
    {
        var events = new List<Event>();
        foreach (var eventName in eventNames)
        {
            eventSource.Register(new DelegateReceptacle(events.Add, eventName));
        }

        return events;
    }

    private Trap<long> CaptureStartingPosition()
    {
        var startingPosition = new Trap<long>();
        positionTracker.Setup(t => t.OnProjectionStarting(It.IsAny<long>()))
            .Callback<long>(l => startingPosition.Value = l);
        return startingPosition;
    }

    private Trap<long> CaptureFinishedPosition()
    {
        var finishedPosition = new Trap<long>();
        positionTracker.Setup(t => t.OnProjectionFinished(It.IsAny<long>()))
            .Callback<long>(l => finishedPosition.Value = l);
        return finishedPosition;
    }

    private void ClearTables()
    {
        connection.Open();
        try
        {
            using var command = connection.CreateCommand("DELETE FROM Events; DELETE FROM Entities;");
            command.ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }
    }
}
