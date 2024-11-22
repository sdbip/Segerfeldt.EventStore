using Segerfeldt.EventStore.Projection.Hosting;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.Tests;

// ReSharper disable once InconsistentNaming
public sealed class EventSourceTests
{
    private Mock<IEventSourceRepository> repository = null!;
    private EventSource eventSource = null!;
    private ReceptacleCollection receptacles = null!;
    private Mock<IPollingStrategy> delayConfiguration = null!;
    private MockProjectionTracker projectionTracker = null!;

    [SetUp]
    public void Setup()
    {
        receptacles = new ReceptacleCollection();
        delayConfiguration = new Mock<IPollingStrategy>();
        projectionTracker = new MockProjectionTracker();
        repository = new Mock<IEventSourceRepository>();
        var targetConnection = new Mock<IDbConnection>();
        targetConnection.Setup(c => c.CreateCommand()).Returns(Mock.Of<IDbCommand>());
        targetConnection.Setup(c => c.BeginTransaction()).Returns(Mock.Of<IDbTransaction>());

        eventSource = new EventSource(repository.Object, new TargetDatabase(() => targetConnection.Object), receptacles, projectionTracker, delayConfiguration.Object);
    }

    [Test]
    public void ReportsEventsWithEntityIdAndDetails()
    {
        repository.Setup(r => r.GetEventsAsync(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync([new Event("an-entity", "some-entity", "first-event", @"{""value"":42}", ordinal: 0, position: 1)]);

        var receivedEvents = CaptureReceivedEvents("first-event");

        ProjectionTester.EmitInitialEvents(eventSource);

        Assert.That(receivedEvents, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(receivedEvents[0].EntityId, Is.EqualTo("an-entity"));
            Assert.That(receivedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(receivedEvents[0].Details, Is.EqualTo(@"{""value"":42}"));
        });
    }

    [Test]
    public void ReportsEventsOrderedByVersion()
    {
        repository.Setup(r => r.GetEventsAsync(-1, It.IsAny<int>()))
            .ReturnsAsync([
                new Event("an-entity", "some-entity", "first-event", @"{""value"":42}", ordinal: 0, position: 0),
                new Event("an-entity", "some-entity", "third-event", @"{""value"":42}", ordinal: 2, position: 0),
                new Event("an-entity", "some-entity", "second-event", @"{""value"":42}", ordinal: 1, position: 0),
            ]);

        var receivedEvents = CaptureReceivedEvents("first-event", "second-event", "third-event");

        ProjectionTester.EmitInitialEvents(eventSource);

        Assert.Multiple(() =>
        {
            Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "first-event", "second-event", "third-event" }));
            Assert.That(receivedEvents.Select(e => e.Name), Is.EqualTo(new[] { "first-event", "second-event", "third-event" }));
        });
    }

    [Test]
    public void ReportsNewPosition()
    {
        repository.Setup(r => r.GetEventsAsync(It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync([new Event("an-entity", "some-entity", "first-event", @"{""value"":42}", ordinal: 0, position: 1)]);

        ProjectionTester.EmitInitialEvents(eventSource);

        Assert.That(projectionTracker.lastProjectedPosition, Is.EqualTo(1));
    }

    private List<Event> CaptureReceivedEvents(params string[] eventNames)
    {
        var events = new List<Event>();
        foreach (var eventName in eventNames)
            receptacles.Add(new DelegateReceptacle(events.Add, eventName));
        return events;
    }
}

public sealed class MockProjectionTracker : IProjectionTracker
{
    public long? lastProjectedPosition;
    public long? GetLastFinishedPosition() => lastProjectedPosition;

    public Task ProjectingPosition(long position, Action runProjection)
    {
        runProjection();
        lastProjectedPosition = position;
        return Task.CompletedTask;
    }
}
