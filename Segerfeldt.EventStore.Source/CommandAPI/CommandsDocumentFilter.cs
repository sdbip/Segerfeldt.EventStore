using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Source.CommandAPI
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CommandsDocumentFilter : IDocumentFilter
    {
        private readonly IEnumerable<Assembly> assemblies;

        public CommandsDocumentFilter(IEnumerable<Assembly> assemblies) => this.assemblies = assemblies;

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var handlerTypes = assemblies
                .SelectMany(assembly => assembly
                    .GetExportedTypes()
                    .Where(type => type.IsClass)
                    .Where(type => !type.IsAbstract)
                    .Where(type => type.GetCustomAttribute<HandlesCommandAttribute>() is not null))
                .GroupBy(type => type.GetCustomAttribute<HandlesCommandAttribute>(false)!.Pattern);

            foreach (var group in handlerTypes)
            {
                var pathItem = new OpenApiPathItem();
                foreach (var handlerType in group)
                {
                    var attribute = handlerType.GetCustomAttribute<HandlesCommandAttribute>(false)!;
                    var operation = CreateOpenApiOperation(handlerType, attribute, context);
                    if (operation is null) continue;

                    pathItem.Operations.Add(attribute.Method, operation);
                }
                swaggerDoc.Paths.Add(group.Key, pathItem);
            }
        }

        private static OpenApiOperation? CreateOpenApiOperation(Type handlerType, HandlesCommandAttribute attribute, DocumentFilterContext context)
        {
            var commandHandlerType = GetCommandHandlerType(handlerType);
            if (commandHandlerType is null) return null;

            var genericArguments = commandHandlerType.GetGenericArguments();
            var commandType = genericArguments[0];
            var dtoType = genericArguments.Skip(1).FirstOrDefault();

            var requestSchema = context.SchemaGenerator.GenerateSchema(commandType, context.SchemaRepository);
            var responseSchema = dtoType is null
                ? new OpenApiSchema()
                : context.SchemaGenerator.GenerateSchema(dtoType, context.SchemaRepository);

            if (!context.SchemaRepository.Schemas.TryGetValue(commandType.FullName!, out var commandSchema))
                context.SchemaRepository.Schemas.TryGetValue(commandType.Name, out commandSchema);

            var operation = new OpenApiOperation
            {
                Summary = commandSchema?.Description,
                OperationId = handlerType.Name,
                Responses = new OpenApiResponses
                {
                    ["200"] = new()
                    {
                        Content =
                        {
                            ["application/json"] = new OpenApiMediaType { Schema = responseSchema, }
                        }
                    }
                },
                Tags =
                {
                    new OpenApiTag { Name = attribute.Entity }
                }
            };

            switch (attribute.Method)
            {
                case OperationType.Get:
                {
                    foreach (var property in commandType.GetProperties())
                    {
                        operation.Parameters.Add(
                            new OpenApiParameter
                            {
                                Name = property.Name,
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema { Type = "string" },
                                Description = commandSchema?.Properties.FirstOrDefault(p => string.Equals(p.Key, property.Name, StringComparison.InvariantCultureIgnoreCase)).Value?.Description
                            });
                    }

                    break;
                }
                case OperationType.Post or OperationType.Put or OperationType.Patch:
                    operation.RequestBody = new OpenApiRequestBody
                    {
                        Content = { ["application/json"] = new OpenApiMediaType { Schema = requestSchema, } }
                    };
                    break;
            }

            if (attribute.Method == OperationType.Delete)
            {
                operation.Parameters.Add(
                    new OpenApiParameter
                    {
                        Name = attribute.EntityIdOrDefault,
                        In = ParameterLocation.Path,
                        Schema = new OpenApiSchema { Type = "string" },
                        Description = $"the entity id of the {attribute.Entity} to delete"
                    });
            }

            else if (attribute.Property is not null)
            {
                operation.Parameters.Add(
                    new OpenApiParameter
                    {
                        Name = attribute.EntityIdOrDefault,
                        In = ParameterLocation.Path,
                        Schema = new OpenApiSchema { Type = "string" },
                        Description = $"the entity id of the {attribute.Entity} to modify"
                    });
            }

            return operation;
        }

        private static Type? GetCommandHandlerType(Type handlerType)
        {
            var type = handlerType;
            while (type is not null && type != typeof(object))
            {
                var handleInterface = type.GetInterfaces().FirstOrDefault(IsCommandHandlerInterface);
                if (handleInterface is not null) return handleInterface;
                type = type.BaseType;
            }

            return null;
        }

        private static bool IsCommandHandlerInterface(Type t) =>
            t.IsGenericType &&
            (t.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
             t.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
    }
}
