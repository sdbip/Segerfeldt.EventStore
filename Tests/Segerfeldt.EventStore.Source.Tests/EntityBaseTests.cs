using System;

using Require = NUnit.Framework.Assert;

namespace Segerfeldt.EventStore.Source.Tests;

public class EntityBaseTests
{
    [Test]
    public void ReplaysEvent()
    {
        var entity = new TestEntity(new EntityId("entity"), EntityVersion.New);
        var @event = PublishedEvent(TestEntity.ReplayAsEvent, "{}");
        entity.ReplayEvents([@event]);

        Require.That(entity.ReplayedEvent, Is.SameAs(@event));
    }

    [Test]
    public void ReplaysEventData()
    {
        var entity = new TestEntity(new EntityId("entity"), EntityVersion.New);
        entity.ReplayEvents(new[] {PublishedEvent(TestEntity.ReplayAsData, @"{""string"":""string"", ""int"":42}")});

        Require.That(entity.ReplayedData, Is.EqualTo(new TestData("string", 42)));
    }

    private static PublishedEvent PublishedEvent(string name, string details) => new(name, details, "actor", DateTimeOffset.UtcNow);

    private class TestEntity : EntityBase
    {
        internal const string ReplayAsEvent = "as-event";
        internal const string ReplayAsData = "as-test-data";

        internal PublishedEvent? ReplayedEvent { get; private set; }
        internal TestData? ReplayedData { get; private set; }

        public TestEntity(EntityId id, EntityVersion version) : base(id, new EntityType("Test"), version) { }

        [ReplaysEvent(ReplayAsEvent)]
        public void ReplayEvent(PublishedEvent @event)
        {
            ReplayedEvent = @event;
        }

        [ReplaysEvent(ReplayAsData)]
        public void ReplayEvent(TestData data)
        {
            ReplayedData = data;
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    // ReSharper disable NotAccessedPositionalProperty.Local
    private record TestData(string String, int Int);
}
