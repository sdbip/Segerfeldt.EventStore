using System;
using System.Data;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.NUnit.Tests;

public sealed class EventSourceExtensionTests
{
    private EventSource eventSource = null!;
    private TestReceptacle receptacle = null!;

    [SetUp]
    public void SetUp()
    {
        var targetConnection = new Mock<IDbConnection>();
        targetConnection.Setup(c => c.CreateCommand()).Returns(Mock.Of<IDbCommand>());
        targetConnection.Setup(c => c.BeginTransaction()).Returns(Mock.Of<IDbTransaction>());
        receptacle = new TestReceptacle();
        eventSource = new EventSource(
            Mock.Of<IEventSourceRepository>(),
            new TargetDatabase(() => targetConnection.Object),
            new ReceptacleCollection([receptacle]),
            new MockProjectionTracker(),
            Mock.Of<IPollingStrategy>());
    }

    [Test]
    public void NotifiesMockedEvent()
    {
        eventSource.MockEmittedEvent("entityId", "Entity", "EventName", new { A = "B" });
        Assert.Multiple(() =>
        {
            Assert.That(receptacle.EntityId, Is.EqualTo("entityId"));
            Assert.That(receptacle.Details, Is.EqualTo(new EventDetails(A: "B")));
        });
    }

    private record EventDetails(string A);

    private class TestReceptacle : ReceptacleBase
    {
        public string? EntityId { get; private set; }
        public EventDetails? Details { get; private set; }

        [ReceivesEvent("EventName", EntityType = "Entity")]
        public void OnEvent(string entityId, EventDetails details)
        {
            EntityId = entityId;
            Details = details;
        }
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
