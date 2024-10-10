using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class CommandInputRequest(Type handlerType, HttpContext context)
{
    private readonly HttpContext context = context;
    private readonly Type handlerType = handlerType;

    public async Task<ActionResult> Execute()
    {
        object command;
        try
        {
            command = await new CommandParser(context)
                .GetCommandDTOAsync(handlerType.GetMethod(nameof(ICommandHandler<object>.Handle))!);
        }
        catch (ParseException exception)
        {
            return new BadRequestObjectResult(exception.ErrorData ?? new {exception.Message});
        }

        return await HandleAsync(command);
    }

    private async Task<ActionResult> HandleAsync(object command)
    {
        var handler = (ICommandHandler)ActivatorUtilities.CreateInstance(context.RequestServices, handlerType);
        var commandHandlerExecuter = new CommandHandlerExecuter(handler);
        return await commandHandlerExecuter.HandleAsync(command, CreateCommandContext());
    }

    private CommandContext CreateCommandContext()
    {
        var factory = context.RequestServices.GetRequiredService<IConnectionFactory>();
        return new CommandContext
        {
            EventPublisher = new EventPublisher(factory),
            EntityStore = new EntityStore(factory),
            HttpContext = context
        };
    }
}
