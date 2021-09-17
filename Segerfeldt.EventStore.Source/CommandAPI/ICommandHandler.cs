using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    public record EmptyCommand;

    public interface ICommandHandler<in TCommand>
    {
        public Task<ActionResult> Handle(TCommand command, HttpContext context);
    }

    public interface ICommandHandler<in TCommand, TDTO>
    {
        public Task<ActionResult<TDTO>> Handle(TCommand command, HttpContext context);
    }
}
