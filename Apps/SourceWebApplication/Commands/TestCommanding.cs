using Segerfeldt.EventStore.Source.CommandAPI;

using System;

using static Segerfeldt.EventStore.Source.CommandAPI.CommandResult;

namespace SourceWebApplication.Commands;

/// <summary>This summary is used to describe the generated endpoint as well as the command DTO.</summary>
public class TestCommanding
{
    /// <summary>This summary is used to describe the `ResultValue` property</summary>
    public string? ResultValue { get; set; }

    [AddsEntity("commanding")]
    internal class TestCommandingCommandHandler : ICommandHandler<TestCommanding, string?>
    {
        public async Task<CommandResult<string?>> Handle(TestCommanding command, CommandContext context)
        {
            await Task.CompletedTask;
            return Ok(command.ResultValue);
        }
    }
}
