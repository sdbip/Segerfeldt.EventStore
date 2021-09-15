using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Text.Json;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    public static class Commanding
    {
        public static void AddCommandsDocumentation(this SwaggerGenOptions swaggerOptions)
        {
            swaggerOptions.DocumentFilter<HistoryDocumentFilter>();
        }

        public static void MapCommands(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("history/{id}", async context =>
            {
                var id = (string?) context.Request.RouteValues["id"];
                var store = context.RequestServices.GetRequiredService<EntityStore>();

                var history = await store.GetHistoryAsync(new EntityId(id!));
                if (history is null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync($"There is no entity with the id '{id}'");
                }
                else
                {
                    var camelCase = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var dto = History.From(history);
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await JsonSerializer.SerializeAsync(context.Response.Body, dto, camelCase);
                }
            });
        }
    }
}
