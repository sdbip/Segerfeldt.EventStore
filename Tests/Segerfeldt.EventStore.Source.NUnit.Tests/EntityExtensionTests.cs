namespace Segerfeldt.EventStore.Source.NUnit.Tests;

public sealed class EntityExtensionTests
{
    [Test]
    public void ReplaysMockedEvent()
    {
        var entity = new TestEntity(new EntityId("test"), EntityVersion.Of(1));
        entity.MockPublishedEvent("EventName", new { A = "B" });
        Assert.That(entity.Details, Is.EqualTo(new EventDetails(A: "B")));
    }

    private class TestEntity(EntityId id, EntityVersion version) : EntityBase(id, new EntityType("test_entity"), version)
    {
        public EventDetails? Details { get; private set; }

        [ReplaysEvent("EventName")]
        public void OnEvent(EventDetails details)
        {
            Details = details;
        }
    }

    private record EventDetails(string A);
}
