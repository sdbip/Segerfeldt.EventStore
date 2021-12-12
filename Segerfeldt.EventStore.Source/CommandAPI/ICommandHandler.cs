using JetBrains.Annotations;

using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    [PublicAPI]
    public sealed record EmptyCommand;

    public interface ICommandHandler { }

    public interface ICommandHandler<in TCommand> : ICommandHandler
    {
        public Task<CommandResult> Handle(TCommand command, CommandContext context);
    }

    public interface ICommandHandler<in TCommand, TResponseDTO> : ICommandHandler
    {
        public Task<CommandResult<TResponseDTO>> Handle(TCommand command, CommandContext context);
    }
}
