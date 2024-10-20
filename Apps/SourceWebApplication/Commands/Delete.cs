using Segerfeldt.EventStore.Source.CommandAPI;

namespace SourceWebApplication.Commands;

/// <summary>This exemplifies a command with a DELETE method.</summary>
public record Delete(string? parameter, string required);

/// <inheritdoc/>
[DeletesEntity("Pokey")]
public sealed class DeleteCommandHandler : ICommandHandler<Delete>
{
    /// <inheritdoc/>
    public Task<CommandResult> Handle(Delete command, CommandContext context) =>
        Task.FromResult(CommandResult.NoContent());
}
