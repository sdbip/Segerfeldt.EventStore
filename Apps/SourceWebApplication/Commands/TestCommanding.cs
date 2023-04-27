using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.CommandAPI;

using System;

using static Segerfeldt.EventStore.Source.CommandAPI.CommandResult;

namespace SourceWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public class TestCommanding
{
    /// <summary>This summary is used to describe the `ResultValue` property</summary>
    public string? ResultValue { get; set; }

    [AddsEntity("test")]
    internal class TestCommandingCommandHandler : ICommandHandler<TestCommanding, string?>
    {
        public async Task<CommandResult<string?>> Handle(TestCommanding command, CommandContext context)
        {
            var entity = TestEntity.New();
            await context.EventPublisher.PublishChangesAsync(entity, "test_user");
            return Ok(command.ResultValue);
        }
    }
}

class TestEntity : EntityBase
{
    public TestEntity(EntityId id, EntityVersion version) : base(id, new EntityType("test"), version) { }

    internal static TestEntity New()
    {
        var entity = new TestEntity(new EntityId(Guid.NewGuid().ToString()), EntityVersion.New);
        entity.Add(new UnpublishedEvent("Test", new { a = 12 }));
        return entity;
    }
}