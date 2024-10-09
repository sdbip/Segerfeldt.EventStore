using Segerfeldt.EventStore.Source.NUnit;

using System;

namespace Segerfeldt.EventStore.Source.Tests;

public class AggregateTests
{
    [Test]
    public void ModifyChild_AddsEvent()
    {
        var aggregate = new Aggregate(new EntityId("test-entity"), EntityVersion.New);
        var child = aggregate.AddChild("child1");
        child.Modify();

        Assert.That(aggregate, Added.Event("ChildModified").WithDetails(new Aggregate.ChildModifiedDetails("child1")));
    }
}

internal class Aggregate(EntityId id, EntityVersion version) : EntityBase(id, new EntityType("Aggregate"), version)
{
    public record ChildModifiedDetails(string id);

    public ChildEntity AddChild(string id) => new(id, Add);

    public class ChildEntity(string id, Action<UnpublishedEvent> addEvent)
    {
        private readonly string id = id;
        private readonly Action<UnpublishedEvent> addEvent = addEvent;

        public void Modify()
        {
            addEvent(new UnpublishedEvent("ChildModified", new ChildModifiedDetails(id)));
        }
    }
}
