using Microsoft.OpenApi.Models;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Segerfeldt.EventStore.Source.CommandAPI;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class HistoryDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc
            .AddPathItem("/history/{entityId}")
            .AddParameter(new OpenApiParameter
            {
                Name = "entityId",
                Description = "The id of the entity to look up",
                In = ParameterLocation.Path,
                Required = true,
                Schema = new OpenApiSchema { Type = "string" }
            })
            .AddOperationWithSuccessResponseType(
                typeof(History),
                "Returns the entire history of an entity");
    }
}
