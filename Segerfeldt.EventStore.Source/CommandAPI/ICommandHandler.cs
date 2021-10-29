using JetBrains.Annotations;

using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    [PublicAPI]
    public sealed record EmptyCommand;

    public interface ICommandHandler { }

    public interface ICommandHandler<in TCommand> : ICommandHandler
    {
        public Task<ActionResult> Handle(TCommand command, CommandContext context);
    }

    public interface ICommandHandler<in TCommand, TResponseDTO> : ICommandHandler
    {
        public Task<ActionResult<TResponseDTO>> Handle(TCommand command, CommandContext context);
    }
}
