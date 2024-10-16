namespace Segerfeldt.EventStore.Source.Tests;

public sealed class IdentifiablesAreSameTests
{
    [Test]
    public void IsSameIfIdMatches()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new TestEntity(EntityId.Value("same_id")).IsSameAs(new TestEntity(EntityId.Value("same_id"))), Is.True, "Same entity");
            Assert.That(new TestEntity(EntityId.Value("the_id")).IsSameAs(new TestEntity(EntityId.Value("other_id"))), Is.False, "Different Id");
            Assert.That(new TestEntity(EntityId.Value("the_id")).IsSameAs(new OtherEntity(EntityId.Value("the_id"))), Is.False, "Different type");
        });
    }

    private record TestEntity(EntityId Id) : IIdentifiable;
    private record OtherEntity(EntityId Id) : IIdentifiable;
}
