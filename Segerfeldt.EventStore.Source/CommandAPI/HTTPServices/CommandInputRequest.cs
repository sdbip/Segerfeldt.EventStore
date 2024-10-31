using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

internal sealed class CommandInputRequest(Type handlerType, HttpContext context)
{
    private readonly HttpContext context = context;
    private readonly Type handlerType = handlerType;

    public async Task<ActionResult> Execute()
    {
        var commandParser = new CommandParser(
            handlerType.GetMethod(nameof(ICommandHandler<object>.Handle))!.GetParameters()[0].ParameterType,
            handlerType.GetCustomAttribute<ModifiesEntityAttribute>()!.SerializationType);

        object command;
        try
        {
            command = await commandParser.GetCommandDTOAsync(context.Request);
        }
        catch (ParseException exception)
        {
            return new BadRequestObjectResult(exception.ErrorData ?? new {exception.Message});
        }

        return await HandleAsync(command);
    }

    private async Task<ActionResult> HandleAsync(object command)
    {
        var handler = ActivatorUtilities.CreateInstance(context.RequestServices, handlerType);
        var commandHandlerExecuter = new CommandHandlerExecuter(handler);
        return await commandHandlerExecuter.HandleAsync(command, CreateCommandContext());
    }

    private CommandContext CreateCommandContext() => context.RequestServices.CreateCommandContext(context);
}

public static class ServiceProviderExtension
{
    internal static CommandContext CreateCommandContext(this IServiceProvider serviceProvider, HttpContext httpContext)
    {
        var factory = serviceProvider.GetRequiredService<EventStoreConnectionFactory>();
        return new CommandContext
        {
            EventPublisher = new EventPublisher(factory),
            EntityStore = new EntityStore(factory),
            HttpContext = httpContext
        };
    }

    /// <summary>Create a CommandContext (for testing)</summary>
    /// <param name="serviceProvider">A service provider that has been set up with <see cref="Commanding.UseEventStore(IServiceCollection, IEventStoreProvider)"/></param>
    public static CommandContext CreateCommandContext(this IServiceProvider serviceProvider) =>
        serviceProvider.CreateCommandContext(new DefaultHttpContext());
}
