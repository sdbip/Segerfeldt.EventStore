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
            var pathItemsByPattern = FindCommandHandlerTypes()
                .GroupBy(handlerType => handlerType.CommandAttribute.Pattern)
                .Select(group => (group.Key, CreateOpenApiPathItem(group, context)));

            foreach (var (pattern, item) in pathItemsByPattern)
                swaggerDoc.Paths.Add(pattern, item);
        }

        private IEnumerable<(Type type, ModifiesEntityAttribute CommandAttribute)> FindCommandHandlerTypes() =>
            assemblies
                .SelectMany(assembly => assembly.GetExportedTypes()
                    .Where(type => type.IsClass && !type.IsAbstract)
                    .Select(type =>
                    {
                        var modifiesEntityAttribute = type.GetCustomAttribute<ModifiesEntityAttribute>(false);
                        if (modifiesEntityAttribute is null) return ((Type, ModifiesEntityAttribute CommandAttribute)?)null;
                        return (type, CommandAttribute: modifiesEntityAttribute);
                    })
                    .RemoveNulls());

        private static OpenApiPathItem CreateOpenApiPathItem(IEnumerable<(Type, ModifiesEntityAttribute)> handlerTypes, DocumentFilterContext context)
        {
            var operations = handlerTypes.Select(handlerType =>
                {
                    var (type, commandAttribute) = handlerType;
                    var commandHandlerType = GetCommandHandlerType(type);
                    if (commandHandlerType is null) return null;

                    var operation = CreateOpenApiOperation(commandAttribute, context, commandHandlerType, type.Name);
                    return ((OperationType method, OpenApiOperation operation)?)(commandAttribute.Method, operation);
                })
                .RemoveNulls()
                .Select(tuple => new KeyValuePair<OperationType, OpenApiOperation>(tuple.method, tuple.operation));

            return new OpenApiPathItem { Operations = new Dictionary<OperationType, OpenApiOperation>(operations) };
        }

        private static OpenApiOperation CreateOpenApiOperation(ModifiesEntityAttribute attribute, DocumentFilterContext context,
            Type commandHandlerType, string operationId)
        {
            var genericArguments = commandHandlerType.GetGenericArguments();
            var commandType = genericArguments[0];
            var dtoType = genericArguments.Skip(1).FirstOrDefault();

            var requestSchema = context.SchemaGenerator.GenerateSchema(commandType, context.SchemaRepository);

            if (!context.SchemaRepository.Schemas.TryGetValue(commandType.FullName!, out var commandSchema))
                context.SchemaRepository.Schemas.TryGetValue(commandType.Name, out commandSchema);

            var operation = new OpenApiOperation
            {
                Summary = commandSchema?.Description,
                OperationId = operationId,
                Responses = new OpenApiResponses { ["200"] = CreateResponseItem(dtoType, context) },
                Tags = { new OpenApiTag { Name = attribute.Entity } }
            };

            switch (attribute.Method)
            {
                case OperationType.Get:
                {
                    foreach (var property in commandType.GetProperties())
                        operation.Parameters.Add(CreateOpenApiParameter(property.Name, commandSchema!));
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
                operation.Parameters.Add(CreateEntityIdParameterItem(attribute, "remove"));
            else if (attribute.Property is not null)
                operation.Parameters.Add(CreateEntityIdParameterItem(attribute, "modify"));

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

        private static OpenApiResponse CreateResponseItem(Type? dtoType, DocumentFilterContext context)
        {
            var schema = dtoType is null
                ? new OpenApiSchema()
                : context.SchemaGenerator.GenerateSchema(dtoType, context.SchemaRepository);
            return new OpenApiResponse { Content = { ["application/json"] = new OpenApiMediaType { Schema = schema } } };
        }

        private static OpenApiParameter CreateOpenApiParameter(string propertyName, OpenApiSchema commandSchema) =>
            new()
            {
                Name = propertyName,
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema { Type = "string" },
                Description = commandSchema.Properties
                    .FirstOrDefault(p => string.Equals(p.Key, propertyName, StringComparison.InvariantCultureIgnoreCase))
                    .Value?.Description
            };

        private static OpenApiParameter CreateEntityIdParameterItem(ModifiesEntityAttribute attribute, string modificationString) =>
            new()
            {
                Name = attribute.EntityIdOrDefault,
                In = ParameterLocation.Path,
                Schema = new OpenApiSchema { Type = "string" },
                Description = $"the entity id of the {attribute.Entity} to {modificationString}"
            };

        private static bool IsCommandHandlerInterface(Type t) =>
            t.IsGenericType &&
            (t.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
             t.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
    }
}
