using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

public class CommandHandlerExecuter(ICommandHandler handler)
{
    private readonly ICommandHandler handler = handler;

    public async Task<ActionResult> ExecuteHandlerAsync(object? command, CommandContext context)
    {
        Task task;
        try
        {
            task = InvokeHandler(handler, command, context);
            await task;
        }
        catch (TargetInvocationException exception)
        {
            if (exception.InnerException is ConcurrentUpdateException inner)
                return new ObjectResult(new { error = inner.Message }) { StatusCode = StatusCodes.Status409Conflict };
            else
                return new ObjectResult(new { error = (exception.InnerException ?? exception).Message }) { StatusCode = StatusCodes.Status500InternalServerError };
        }

        // The correct type is Task<T> (where T: ICommandResult), but since we do not know T at this point,
        // we cannot bind to that type. We cannot even cast to Task<ICommandType>. Instead we have to access
        // the Result property through reflection.
        var handlerResult = (ICommandResult)task.GetPropertyValue(nameof(Task<object>.Result))!;
        return handlerResult.ActionResult();
    }

    private static Task InvokeHandler(ICommandHandler handler, object? command, CommandContext context)
    {
        // The handler might be any of ICommandHandler<in TCommand>, ICommandHandler<in TCommand, TResponseDTO>,
        // ICommandlessHandler or ICommandlessHandler<TResponseDTO>. The exact type cannot be known. It is
        // therefore impossible to access the Handle method in a type-secure manner. It has to be invoked
        // through reflection.
        var parameters = command is null ? new[] { context } : new[] { command, context };

        // The response type depends on which interface is implemented. All we can say for sure is that it is
        // a Task<T> (which inherits Task), but we cannot tell what the generic argument T is.
        return (Task)handler.InvokeMethod(nameof(ICommandHandler<object>.Handle), parameters)!;
    }
}
