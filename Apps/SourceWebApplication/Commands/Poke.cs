using Segerfeldt.EventStore.Source.CommandAPI;

namespace SourceWebApplication.Commands;

/// <summary>This exemplifies a command with a custom method.</summary>
/// NOTE: Swagger doesn't support custom methods so this command will not be documented.
public record Poke(string stick);

/// <inheritdoc/>
[
    ModifiesEntity("Pokey",
        IncludeEntityId = false,
        CustomMethod = "POKE",
        SerializationType = CommandSerializationMode.URLQuery)
]
public sealed class PokeCommandHandler : ICommandHandler<Poke>
{
    /// <inheritdoc/>
    public Task<CommandResult> Handle(Poke command, CommandContext context) =>
        Task.FromResult(CommandResult.NoContent());
}
