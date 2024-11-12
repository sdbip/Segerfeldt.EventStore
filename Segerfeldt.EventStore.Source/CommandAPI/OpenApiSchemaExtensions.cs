using Microsoft.OpenApi.Models;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Segerfeldt.EventStore.Source.CommandAPI;

internal static class OpenApiSchemaExtensions
{
    public static OpenApiPathItem AddPathItem(this OpenApiDocument swaggerDoc, string path)
    {
        var pathItem = new OpenApiPathItem();
        swaggerDoc.Paths.Add(path, pathItem);
        return pathItem;
    }

    public static OpenApiPathItem AddParameter(this OpenApiPathItem pathItem, OpenApiParameter parameter)
    {
        pathItem.Parameters.Add(parameter);
        return pathItem;
    }

    public static void AddOperationWithSuccessResponseType(this OpenApiPathItem pathItem, Type responseType, string summary)
    {
        var operation = new OpenApiOperation { Summary = summary };
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
                        Schema = CreateSchema(responseType)
                    }
                }
            }
        });


        pathItem.AddOperation(OperationType.Get, operation);
    }

    private static OpenApiSchema CreateSchema(Type type)
    {
        var result = new OpenApiSchema
        {
            AdditionalPropertiesAllowed = true,
            Type = type == typeof(bool) ? "boolean" :
                IsNumber(type) ? "number" :
                type == typeof(string) ? "string" :
                type.IsAssignableTo(typeof(IEnumerable)) ? "array" :
                "object"
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
