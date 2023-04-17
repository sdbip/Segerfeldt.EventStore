using Segerfeldt.EventStore.Source.CommandAPI;

using System;

using static Segerfeldt.EventStore.Source.CommandAPI.CommandResult;

namespace SourceWebApplication.Commands;

public class TestCommanding
{
    public string? ResultValue { get; set; }
}

[AddsEntity("commanding")]
public class AddActivistCommandHandler : ICommandHandler<TestCommanding, string?>
{
    public async Task<CommandResult<string?>> Handle(TestCommanding command, CommandContext context)
    {
        await Task.CompletedTask;
        return Ok(command.ResultValue);
    }
}
