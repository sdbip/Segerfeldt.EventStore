using JetBrains.Annotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI;

[PublicAPI]
public static class Commanding
{
    public static SwaggerGenOptions DocumentCommands(this SwaggerGenOptions swaggerOptions, params Assembly[] assemblies)
    /// <summary>Add Swagger documentation for command handlers from their XML documentation</summary>
    /// <param name="assemblies">assemblies to search for command definitions</param>
    /// <param name="swaggerOptions">the Swagger configuration to modify</param>
    {
        swaggerOptions.DocumentFilter<HistoryDocumentFilter>();
        swaggerOptions.DocumentFilter<CommandsDocumentFilter>(assemblies.AsEnumerable());
        return swaggerOptions;
    }

    /// <summary>Map endpoints to command handlers</summary>
    /// <param name="builder">the web-app configuration to modify</param>
    /// <param name="assemblies">assemblies to search for command definitions</param>
    public static IEndpointRouteBuilder MapCommands(this IEndpointRouteBuilder builder, params Assembly[] assemblies)
    {
        builder.MapHistory();
        builder.MapCommandHandlers(assemblies);
        return builder;
    }

    private static void MapHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("history/{entityId}", GetHistory);
    }

    private static async Task GetHistory(HttpContext context)
    {
        var result = await new HistoryQueryRequest(context).Get();
        await SendResponse(context, result);
    }

    private static void MapCommandHandlers(this IEndpointRouteBuilder endpoints, Assembly[] assemblies)
    {
        var attributedClasses = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetCustomAttribute<ModifiesEntityAttribute>(false) is not null)
            .Select(type => (type, type.GetCustomAttribute<ModifiesEntityAttribute>(false)!));

        foreach (var (handlerClass, attribute) in attributedClasses)
            endpoints.MapMethods(attribute.Pattern, [attribute.Method.ToString()], context => HandleCommand(context, handlerClass));
    }

    private static async Task HandleCommand(HttpContext context, TypeInfo handlerClass)
    {
        var result = await new CommandInputRequest(handlerClass, context).Execute();
        await SendResponse(context, result);
    }

    private static async Task SendResponse(HttpContext context, ActionResult response)
    {
        await response.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
    }
}
