using NUnit.Framework;

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

internal class Aggregate : EntityBase
{
    public Aggregate(EntityId id, EntityVersion version) : base(id, new EntityType("Aggregate"), version) { }

    public record ChildModifiedDetails(string id);

    public ChildEntity AddChild(string id)
    {
        return new ChildEntity(id, Add);
    }

    public class ChildEntity
    {
        private readonly string id;
        private readonly Action<UnpublishedEvent> addEvent;

        public ChildEntity(string id, Action<UnpublishedEvent> addEvent)
        {
            this.id = id;
            this.addEvent = addEvent;
        }

        public void Modify()
        {
            addEvent(new UnpublishedEvent("ChildModified", new ChildModifiedDetails(id)));
        }
    }
}
