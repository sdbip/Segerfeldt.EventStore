using Microsoft.OpenApi.Models;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Segerfeldt.EventStore.Source.CommandAPI;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class ProjectionDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var operation = new OpenApiOperation { Summary = "Returns public events for the next few positions (if any have been published)" };
        operation.Tags.Add(new OpenApiTag { Name = "History" });

        operation.Responses.Add("200", new OpenApiResponse
        {
            Description = "Success",
            Content =
            {
                {
                    "application/json",
                    new OpenApiMediaType
                    {
                        Schema = CreateSchema(typeof(IEnumerable<ProjectionPosition>))
                    }
                }
            }
        });


        var pathItem = new OpenApiPathItem();
        pathItem.Parameters.Add(new OpenApiParameter
        {
            Name = "after",
            Description = "Optional position that sets a lower bound. No events on or before this position will be returned.",
            In = ParameterLocation.Query,
            Required = false,
            Schema = new OpenApiSchema { Type = "long" }
        });
        pathItem.AddOperation(OperationType.Get, operation);
        swaggerDoc.Paths.Add("/history", pathItem);
    }

    private static OpenApiSchema CreateSchema(Type type)
    {
        var result = new OpenApiSchema
        {
            AdditionalPropertiesAllowed = true,
            Type = type == typeof(bool) ? "boolean" : IsNumber(type) ? "number" : type == typeof(string) ? "string" : type.IsAssignableTo(typeof(IEnumerable)) ? "array" : "object"
        };

        if (IsPrimitive(type)) return result;
        if (type.IsAssignableTo(typeof(JsonElement))) return result;
        if (type.IsAssignableTo(typeof(IDictionary<string, object>))) return result;

        if (type.IsAssignableTo(typeof(IEnumerable)))
        {
            var genericInterface = type.GetInterfaces().Prepend(type).FirstOrDefault(t => t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            result.Items = genericInterface is null ? new OpenApiSchema() : CreateSchema(genericInterface.GetGenericArguments()[0]);
        }
        else
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var camelName = $"{char.ToLower(property.Name[0])}{property.Name[1..]}";
                result.Properties[camelName] = CreateSchema(property.PropertyType);
            }
        }

        return result;
    }

    private static bool IsPrimitive(Type type) => type == typeof(string) || type == typeof(bool) || IsNumber(type);
    private static bool IsNumber(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(byte) || type == typeof(short) ||
        type == typeof(double) || type == typeof(decimal) || type == typeof(float);
}
