using Microsoft.AspNetCore.Mvc;

using NUnit.Framework;

using Segerfeldt.EventStore.Source.Tests;

using static Segerfeldt.EventStore.Source.CommandAPI.CommandResult;

namespace Segerfeldt.EventStore.Source.NUnit.Tests;

public sealed class EntityExtensionTests
{
    [Test]
    public void Error_ConvertsToActionResultWithMessage()
    {
        var entity = new TestEntity(new EntityId("test"), EntityVersion.Of(1));
        entity.MockPublishedEvent("EventName", new { A = "B" });
        Assert.That(entity.Details, Is.EqualTo(new EventDetails(A: "B")));
    }

    private class TestEntity : EntityBase
    {
        public EventDetails? Details { get; private set; }

        public TestEntity(EntityId id, EntityVersion version) : base(id, new EntityType("test_entity"), version) { }


        [ReplaysEvent("EventName")]
        public void OnEvent(EventDetails details)
        {
            Details = details;
        }
    }

    private record EventDetails(string A);
}
