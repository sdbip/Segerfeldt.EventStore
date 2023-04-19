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
    public static void DocumentCommands(this SwaggerGenOptions swaggerOptions, params Assembly[] assemblies)
    {
        swaggerOptions.DocumentFilter<HistoryDocumentFilter>();
        swaggerOptions.DocumentFilter<CommandsDocumentFilter>(assemblies.AsEnumerable());
    }

    public static void MapCommands(this IEndpointRouteBuilder endpoints, params Assembly[] assemblies)
    {
        endpoints.MapHistory();
        endpoints.MapCommandHandlers(assemblies);
    }

    private static void MapHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("history/{entityId}", GetHistory);
    }

    private static async Task GetHistory(HttpContext context)
    {
        var result = await new QueryHandler(context).GetHistory();
        await new ResponseBuilder(context).ApplyResult(result);
    }

    private static void MapCommandHandlers(this IEndpointRouteBuilder endpoints, Assembly[] assemblies)
    {
        var attributedClasses = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetCustomAttribute<ModifiesEntityAttribute>(false) is not null)
            .Select(type => (type, type.GetCustomAttribute<ModifiesEntityAttribute>(false)!));

        foreach (var (handlerClass, attribute) in attributedClasses)
            endpoints.MapMethods(attribute.Pattern, new[] { attribute.Method.ToString() }, context => HandleCommand(context, handlerClass));
    }

    private static async Task HandleCommand(HttpContext context, TypeInfo handlerClass)
    {
        var result = await new CommandHandler(context).HandleCommand(handlerClass);
        await new ResponseBuilder(context).ApplyResult(result);
    }

    private class ResponseBuilder
    {
        private readonly HttpContext context;

        public ResponseBuilder(HttpContext context)
        {
            this.context = context;
        }

        public async Task ApplyResult(IActionResult actionResult)
        {
            await actionResult.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
        }
    }
}
