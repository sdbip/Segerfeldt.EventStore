using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Source.CommandAPI;

public sealed class DocumentationGenerator
{
    private readonly HashSet<Type> commandHandlers = new();
    private readonly DocumentFilterContext context;

    public DocumentationGenerator(DocumentFilterContext context)
    {
        this.context = context;
    }

    public void AddCommandHandler(Type type)
    {
        commandHandlers.Add(type);
    }

    public void Generate(OpenApiDocument document)
    {
        foreach (var handler in commandHandlers)
        {
            context.SchemaGenerator.GenerateSchema(handler, context.SchemaRepository);

            var commandParameter = handler.GetMethod("Handle")?.GetParameters().First();
            if (commandParameter?.ParameterType == typeof(CommandContext))
                commandParameter = null;
            var requestBodySchema = commandParameter is null ? null :
                context.SchemaGenerator.GenerateSchema(commandParameter.ParameterType, context.SchemaRepository);

            var commandSchema = requestBodySchema is null
                ? context.SchemaRepository.Schemas[handler.Name]
                : context.SchemaRepository.Schemas[requestBodySchema.Reference.Id];

            var attribute = handler.GetCustomAttribute<ModifiesEntityAttribute>(false)!;
            document.Paths[attribute.Pattern] = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    {
                        attribute.Method,
                        new OpenApiOperation
                        {
                            Parameters = Parameters(attribute),
                            OperationId = $"{attribute.Method.ToString().ToUpper()} {attribute.Pattern}",
                            Summary = commandSchema.Description,
                            Responses = Responses(handler, context),
                            RequestBody = RequestBody(commandParameter?.ParameterType, requestBodySchema),
                            Tags = { new OpenApiTag { Name = attribute.Entity } }
                        }
                    },
                }
            };
        }
    }

    private static IList<OpenApiParameter>? Parameters(ModifiesEntityAttribute attribute)
    {
        if (!attribute.HasEntityIdParameter && !attribute.HasPropertyIdParameter) return null;

        var result = new List<OpenApiParameter>();
        if (attribute.HasEntityIdParameter)
        {
            var description = attribute.Method == OperationType.Delete && !attribute.HasPropertyIdParameter
                ? $"the entity id of the {attribute.Entity} to delete"
                : $"the entity id of the {attribute.Entity} to modify";
            result.Add(new OpenApiParameter { Name = attribute.EntityIdOrDefault, In = ParameterLocation.Path, Schema = new OpenApiSchema { Type = "string" }, Description = description });
        }
        if (attribute.HasPropertyIdParameter)
        {
            var description = attribute.Method == OperationType.Delete
                ? $"the entity id of the {attribute.Property} to remove"
                : $"the entity id of the {attribute.Property} to modify";
            result.Add(new OpenApiParameter { Name = attribute.PropertyId, In = ParameterLocation.Path, Schema = new OpenApiSchema { Type = "string" }, Description = description });
        }

        return result;
    }

    private static OpenApiRequestBody? RequestBody(Type? parameterType, OpenApiSchema? requestBodySchema)
    {
        if (parameterType is null) return null;

        return new OpenApiRequestBody
        {
            Content = { { "application/json", new OpenApiMediaType { Schema = new OpenApiSchema { Reference = new OpenApiReference { Id = parameterType.Name, Type = ReferenceType.Schema } } } } },
        };
    }

    private static OpenApiResponses Responses(Type handler, DocumentFilterContext context)
    {
        var responseType = GetResponseType(handler);
        if (responseType is null) return new OpenApiResponses
        {
            ["204"] = new OpenApiResponse()
        };

        var responseSchema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository);

        return new OpenApiResponses
        {
            ["200"] = new OpenApiResponse
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    { "application/json", new OpenApiMediaType { Schema = responseSchema } }
                }
            }
        };
    }

    private static Type? GetResponseType(Type handler)
    {
        var responseType = handler.GetMethod("Handle")?.ReturnType // Task<CommandResult<T>>
            .GetGenericArguments().First()                         // CommandResult<T>
            .GetGenericArguments().FirstOrDefault();               // T
        return responseType;
    }
}
