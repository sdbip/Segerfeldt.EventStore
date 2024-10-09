using Moq;

using NUnit.Framework;

using Segerfeldt.EventStore.Tests.Shared;

namespace Segerfeldt.EventStore.Projection.NUnit.Tests;

public sealed class EventSourceExtensionTests
{
    private EventSource eventSource = null!;
    private TestReceptacle receptacle = null!;

    [SetUp]
    public void SetUp()
    {
        var connectionPool = new Mock<IConnectionPool>();
        connectionPool.Setup(pool => pool.CreateConnection()).Returns(new InMemoryConnection());
        eventSource = new EventSource(new DefaultEventSourceRepository(connectionPool.Object), Mock.Of<IProjectionTracker>(), Mock.Of<IPollingStrategy>());

        receptacle = new TestReceptacle();
        eventSource.Register(receptacle);
    }

    [Test]
    public void NotifiesMockedEvent()
    {
        eventSource.MockNotifiedEvent("entityId", "Entity", "EventName", new { A = "B" });
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
