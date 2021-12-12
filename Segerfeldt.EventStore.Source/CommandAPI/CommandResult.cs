using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    public interface ICommandResult
    {
        public ActionResult ActionResult { get; }
    }

    public class CommandResult : ICommandResult
    {
        public ActionResult ActionResult { get; }

        public CommandResult(ActionResult actionResult) => ActionResult = actionResult;

        public static CommandResult NoContent() => new(new NoContentResult());
        public static CommandResult NotModified() => new(new StatusCodeResult(StatusCodes.Status304NotModified));
        public static CommandResult<T> Ok<T>(T value) => value;

        public static CommandResult BadRequest(string error) => new(new BadRequestObjectResult(error));
        public static CommandResult NotFound(string error) => new(new NotFoundObjectResult(error));

        public static CommandResult Forbidden() => new(new ForbidResult());
        public static CommandResult Unauthorized() => new(new UnauthorizedResult());
    }

    public class CommandResult<T> : ICommandResult
    {
        public ActionResult ActionResult { get; }

        private CommandResult(ActionResult actionResult) => ActionResult = actionResult;

        public static implicit operator CommandResult<T>(T value) => new(new OkObjectResult(value));
        public static implicit operator CommandResult<T>(CommandResult value) => new(value.ActionResult);
    }
}
