namespace Segerfeldt.EventStore.Source.Tests;

public sealed class IdentifiablesAreSameTests
{
    [Test]
    public void IsSameIfIdMatches()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new TestEntity(EntityId.Value("same_id").OrThrow()).IsSameAs(new TestEntity(EntityId.Value("same_id").OrThrow())), Is.True, "Same entity");
            Assert.That(new TestEntity(EntityId.Value("the_id").OrThrow()).IsSameAs(new TestEntity(EntityId.Value("other_id").OrThrow())), Is.False, "Different Id");
            Assert.That(new TestEntity(EntityId.Value("the_id").OrThrow()).IsSameAs(new OtherEntity(EntityId.Value("the_id").OrThrow())), Is.False, "Different type");
        });
    }

    private record TestEntity(EntityId Id) : IIdentifiable;
    private record OtherEntity(EntityId Id) : IIdentifiable;
}
