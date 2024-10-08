using Moq;

using NUnit.Framework;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Segerfeldt.EventStore.Projection.Tests;

// ReSharper disable once InconsistentNaming
public class SQLiteProjectionTests
{
    private InMemoryConnection connection = null!;
    private EventSource eventSource = null!;
    private Mock<IPollingStrategy> delayConfiguration = null!;
    private Mock<IPositionTracker> positionTracker = null!;

    [SetUp]
    public void Setup()
    {
        connection = new InMemoryConnection();
        var connectionPool = new Mock<IConnectionPool>();
        connectionPool.Setup(pool => pool.CreateConnection()).Returns(connection);
        delayConfiguration = new Mock<IPollingStrategy>();
        positionTracker = new Mock<IPositionTracker>();
        eventSource = new EventSource(new DefaultEventSourceRepository(connectionPool.Object), positionTracker.Object, delayConfiguration.Object);

        Source.SQLite.Schema.CreateIfMissing(connection);
    }

    [Test]
    public void ReportsEventsWithEntityIdAndDetails()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", @"{""value"":42}");

        var receivedEvents = CaptureReceivedEvents("first-event");

        eventSource.BeginProjecting();

        Assert.That(receivedEvents, Is.Not.Empty);
        Assert.That(receivedEvents[0].EntityId, Is.EqualTo("an-entity"));
        Assert.That(receivedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(receivedEvents[0].Details, Is.EqualTo(@"{""value"":42}"));
    }

    [Test]
    public void ReportsEventsOrderedByVersion()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", ordinal: 1);
        GivenEvent("an-entity", "third-event", ordinal: 3);
        GivenEvent("an-entity", "second-event", ordinal: 2);

        var receivedEvents = CaptureReceivedEvents("first-event", "second-event", "third-event");

        eventSource.BeginProjecting();

        Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "first-event", "second-event", "third-event" }));
        Assert.That(receivedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(receivedEvents[1].Name, Is.EqualTo("second-event"));
        Assert.That(receivedEvents[2].Name, Is.EqualTo("third-event"));
    }

    [Test]
    public void NotifiesNewEvents()
    {
        delayConfiguration.Setup(c => c.NextDelay(It.IsAny<int>())).Returns(1);

        GivenEntity("an-entity");
        var receivedEvents = CaptureReceivedEvents("early-event", "late-event");
        GivenEvent("an-entity", "early-event", ordinal: 1, position: 1);
        eventSource.BeginProjecting();
        receivedEvents.Clear();

        GivenEvent("an-entity", "late-event", ordinal: 2, position: 2);

        Thread.Sleep(100);

        Assert.That(receivedEvents, Is.Not.Empty);
        Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "late-event" }));
    }

    [Test]
    public void AllowsSettingStartPosition()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", position: 32);
        GivenEvent("an-entity", "second-event", position: 33);
        positionTracker.Setup(t => t.GetLastFinishedProjectionId()).Returns(32);

        var receivedEvents = CaptureReceivedEvents("first-event", "second-event");

        eventSource.BeginProjecting();

        Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "second-event" }));
    }

    [Test]
    public void ReportsNewPosition()
    {
        var startingPosition = CaptureStartingPosition();
        var finishedPosition = CaptureFinishedPosition();

        GivenEntity("an-entity");
        GivenEvent("an-entity", "an-event", position: 1);
        eventSource.BeginProjecting();

        Assert.That(startingPosition.Value, Is.EqualTo(1));
        Assert.That(finishedPosition.Value, Is.EqualTo(1));
    }

    private void GivenEntity(string entityId)
    {
        var command = connection.CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@entityId, 'a-type', 2)");
        command.AddParameter("@entityId", entityId);
        command.ExecuteNonQuery();
    }

    private void GivenEvent(string entityId, string eventName, string details = "{}", int ordinal = 1, long position = 1)
    {
        var command = connection.CreateCommand(
            @"INSERT INTO Events (entity_id, name, details, actor, ordinal, position)
                    VALUES (@entityId, @eventName, @details, 'test', @ordinal, @position)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@eventName", eventName);
        command.AddParameter("@details", details);
        command.AddParameter("@ordinal", ordinal);
        command.AddParameter("@position", position);
        command.ExecuteNonQuery();
    }

    private List<Event> CaptureReceivedEvents(params string[] eventNames)
    {
        var events = new List<Event>();
        foreach (var eventName in eventNames)
            eventSource.Register(new DelegateReceptacle(events.Add, eventName));
        return events;
    }

    private Trap<long> CaptureFinishedPosition()
    {
        var finishedPosition = new Trap<long>();
        positionTracker.Setup(t => t.OnProjectionStarting(It.IsAny<long>()))
            .Callback<long>(l => finishedPosition.Value = l);
        return finishedPosition;
    }

    private Trap<long> CaptureStartingPosition()
    {
        var startingPosition = new Trap<long>();
        positionTracker.Setup(t => t.OnProjectionFinished(It.IsAny<long>()))
            .Callback<long>(l => startingPosition.Value = l);
        return startingPosition;
    }
}
