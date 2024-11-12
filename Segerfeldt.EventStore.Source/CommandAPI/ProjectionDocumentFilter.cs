using Microsoft.OpenApi.Models;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using Swashbuckle.AspNetCore.SwaggerGen;

using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source.CommandAPI;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class ProjectionDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc
            .AddPathItem("/history")
            .AddParameter(new OpenApiParameter
            {
                Name = "after",
                Description = "Optional position that sets a lower bound. No events on or before this position will be returned.",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema { Type = "long" }
            })
            .AddOperationWithSuccessResponseType(
                typeof(IEnumerable<ProjectionPosition>),
                "Returns public events for the next few positions (if any have been published)");
    }
}
