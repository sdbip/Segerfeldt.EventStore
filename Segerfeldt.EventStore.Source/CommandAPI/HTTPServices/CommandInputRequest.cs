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

        var commandHandlerExecuter = new CommandHandlerExecuter((ICommandHandler)ActivatorUtilities.CreateInstance(context.RequestServices, handlerType));
        var commandContext = new CommandContext
        {
            EventPublisher = new EventPublisher(context.RequestServices.GetRequiredService<IConnectionFactory>()),
            EntityStore = new EntityStore(context.RequestServices.GetRequiredService<IConnectionFactory>()),
            HttpContext = context
        };

        return await commandHandlerExecuter.ExecuteHandlerAsync(command, commandContext);
    }
}
