using System.Collections.Generic;
using System.Linq;

namespace Segerfeldt.EventStore.Projection.Tests;

// ReSharper disable once InconsistentNaming
public class EventSourceTests
{
    private Mock<IEventSourceRepository> repository = null!;
    private EventSource eventSource = null!;
    private Mock<IPollingStrategy> delayConfiguration = null!;
    private Mock<IProjectionTracker> positionTracker = null!;

    [SetUp]
    public void Setup()
    {
        repository = new Mock<IEventSourceRepository>();
        delayConfiguration = new Mock<IPollingStrategy>();
        positionTracker = new Mock<IProjectionTracker>();
        eventSource = new EventSource(repository.Object, positionTracker.Object, delayConfiguration.Object);
    }

    [Test]
    public void ReportsEventsWithEntityIdAndDetails()
    {
        repository.Setup(r => r.GetEvents(It.IsAny<long>()))
            .Returns([new Event("an-entity", "first-event", "some-entity", @"{""value"":42}", ordinal: 0, position: 1)]);

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
        repository.Setup(r => r.GetEvents(-1))
            .Returns([
                new Event("an-entity", "first-event", "some-entity", @"{""value"":42}", ordinal: 0, position: 0),
                new Event("an-entity", "third-event", "some-entity", @"{""value"":42}", ordinal: 2, position: 0),
                new Event("an-entity", "second-event", "some-entity", @"{""value"":42}", ordinal: 1, position: 0),
            ]);

        var receivedEvents = CaptureReceivedEvents("first-event", "second-event", "third-event");

        eventSource.BeginProjecting();

        Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "first-event", "second-event", "third-event" }));
        Assert.That(receivedEvents.Select(e => e.Name), Is.EqualTo(new[] { "first-event", "second-event", "third-event" }));
    }

    [Test]
    public void ReportsNewPosition()
    {
        repository.Setup(r => r.GetEvents(It.IsAny<long>()))
            .Returns([new Event("an-entity", "first-event", "some-entity", @"{""value"":42}", ordinal: 0, position: 1)]);

        var startingPosition = CaptureStartingPosition();
        var finishedPosition = CaptureFinishedPosition();

        eventSource.BeginProjecting();

        Assert.That(startingPosition.Value, Is.EqualTo(1));
        Assert.That(finishedPosition.Value, Is.EqualTo(1));
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
