namespace Segerfeldt.EventStore.Source.Tests;

public sealed class IdentifiablesAreSameTests
{
    [Test]
    public void IsSameIfIdMatches()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new TestEntity(new EntityId("same_id")).IsSameAs(new TestEntity(new EntityId("same_id"))), Is.True, "Same entity");
            Assert.That(new TestEntity(new EntityId("the_id")).IsSameAs(new TestEntity(new EntityId("other_id"))), Is.False, "Different Id");
            Assert.That(new TestEntity(new EntityId("the_id")).IsSameAs(new OtherEntity(new EntityId("the_id"))), Is.False, "Different type");
        });
    }

    private record TestEntity(EntityId Id) : IIdentifiable;
    private record OtherEntity(EntityId Id) : IIdentifiable;
}
