using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal class CommandInputRequest(Type handlerType, HttpContext context)
{
    private readonly HttpContext context = context;
    private readonly Type handlerType = handlerType;

    public async Task<ActionResult> Execute()
    {
        var serviceLocator = new ServiceLocator(context.RequestServices);
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

        var commandHandlerExecuter = new CommandHandlerExecuter((ICommandHandler)serviceLocator.CreateInstance(handlerType));
        var commandContext = new CommandContext
        {
            EventPublisher = serviceLocator.GetServiceOrCreateInstance<EventPublisher>(),
            EntityStore = serviceLocator.GetServiceOrCreateInstance<EntityStore>(),
            HttpContext = context
        };

        return await commandHandlerExecuter.ExecuteHandlerAsync(command, commandContext);
    }
}
