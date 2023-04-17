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
        endpoints.MapGet("history/{entityId}", async context =>
        {
            var result = await new QueryHandler(context).GetHistory();
            await new ResponseBuilder(context).ApplyResult(result);
        });

        var handlerTypes = assemblies
            .SelectMany(assembly => assembly
                .GetExportedTypes()
                .Where(type => type.IsClass)
                .Where(type => !type.IsAbstract));

        foreach (var handlerType in handlerTypes)
        {
            if (handlerType.GetCustomAttribute<ModifiesEntityAttribute>(false) is not { } attribute) continue;
            var pattern = attribute.Pattern;
            endpoints.MapMethods(pattern, new []{attribute.Method.ToString()}, async context => {
                var result = await new CommandHandler(context).HandleCommand(handlerType);
                await new ResponseBuilder(context).ApplyResult(result);
            });
        }
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
