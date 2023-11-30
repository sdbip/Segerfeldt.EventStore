using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Source.CommandAPI;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class CommandsDocumentFilter : IDocumentFilter
{
    private readonly IEnumerable<Assembly> assemblies;

    public CommandsDocumentFilter(IEnumerable<Assembly> assemblies) => this.assemblies = assemblies;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var pathItemsByPattern = FindCommandHandlerTypes()
            .GroupBy(handlerType => handlerType.CommandAttribute.Pattern)
            .Select(group => (group.Key, CreateOpenApiPathItem(group, context)));

        foreach (var (pattern, item) in pathItemsByPattern)
          if (swaggerDoc.Paths.ContainsKey(pattern))
          {
            var existingDoc = swaggerDoc.Paths[pattern];
            foreach (var operation in item.Operations)
            {
              if (existingDoc.Operations.ContainsKey(operation.Key))
                throw new Exception($"The operation '{operation.Key}' has multiple definitions for pattern '{pattern}'");
              existingDoc.Operations.Add(operation.Key, operation.Value);
            }
          }
          else
          {
              swaggerDoc.Paths.Add(pattern, item);
          }
    }

    private IEnumerable<CommandHandlerType> FindCommandHandlerTypes() =>
        assemblies
            .SelectMany(assembly => assembly.DefinedTypes
                .Where(type => type.IsClass && !type.IsAbstract)
                .Select(type =>
                {
                    var modifiesEntityAttribute = type.GetCustomAttribute<ModifiesEntityAttribute>(false);
                    if (modifiesEntityAttribute is null) return (CommandHandlerType?)null;
                    return new CommandHandlerType { Type = type, CommandAttribute = modifiesEntityAttribute };
                })
                .RemoveNulls());

    private static OpenApiPathItem CreateOpenApiPathItem(IEnumerable<CommandHandlerType> handlerTypes, DocumentFilterContext context) =>
        new OpenApiPathItem
        {
            Operations = handlerTypes
                .Select(handlerType =>
                    (handlerType, interfaceType: GetAncestors(handlerType.Type)
                        .Select(t => t.GetInterfaces().FirstOrDefault(IsCommandHandlerInterface))
                        .FirstOrDefault(t => t is not null)))
                .Where(t => t.interfaceType is not null)
                .ToDictionary(
                    t => t.handlerType.CommandAttribute.Method,
                    t => CreateOpenApiOperation(t.handlerType, context, t.interfaceType!, "t.handlerType.Type.Name"))
        };

    private static IEnumerable<Type> GetAncestors(Type handlerType)
    {
        var type = handlerType;
        while (type is not null)
        {
            yield return type;
            type = type.BaseType;
        }
    }

    private static OpenApiOperation CreateOpenApiOperation(CommandHandlerType commandHandlerType, DocumentFilterContext context,
        Type interfaceType, string operationId)
    {
        var attribute = commandHandlerType.CommandAttribute;

        var isCommandless = IsCommandlessHandlerInterface(interfaceType);
        var isEmptyCommand = !isCommandless && interfaceType.GetGenericArguments().First() == typeof(EmptyCommand);

        var commandType = isCommandless || isEmptyCommand
            ? commandHandlerType.Type
            : interfaceType.GetGenericArguments().First();

        var dtoType = isCommandless
            ? interfaceType.GetGenericArguments().FirstOrDefault()
            : interfaceType.GetGenericArguments().Skip(1).FirstOrDefault();

        var properties = isCommandless || isEmptyCommand
            ? Array.Empty<PropertyInfo>()
            : commandType.GetProperties();

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
                foreach (var property in properties)
                {
                    var description = commandSchema!.Properties
                        .FirstOrDefault(p =>
                            string.Equals(p.Key, property.Name, StringComparison.InvariantCultureIgnoreCase))
                        .Value.Description;
                    operation.Parameters.Add(CreateOpenApiParameter(property.Name, ParameterLocation.Query, description));
                }
                break;

            case OperationType.Post or OperationType.Put or OperationType.Patch:
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = { ["application/json"] = new OpenApiMediaType { Schema = requestSchema, } }
                };
                break;
        }

        if (attribute.HasEntityIdParameter)
            AddEntityIdProperty(attribute, operation);

        if (attribute.HasPropertyIdParameter)
            AddPropertyParameter(attribute, operation);

        return operation;
    }

    private static void AddEntityIdProperty(ModifiesEntityAttribute attribute, OpenApiOperation operation)
    {
        var description = attribute.Method == OperationType.Delete
            ? $"the entity id of the {attribute.Entity} to remove"
            : $"the entity id of the {attribute.Entity} to modify";

        operation.Parameters.Add(CreateOpenApiParameter(attribute.EntityIdOrDefault, ParameterLocation.Path, description));
    }

    private static void AddPropertyParameter(ModifiesEntityAttribute attribute, OpenApiOperation operation)
    {
        if (attribute.PropertyId is null) return;

        var modify = attribute.Method == OperationType.Delete ? "remove" : "modify";
        var description = $"the associated {attribute.PropertyId} to {modify} for the {attribute.Entity}";

        operation.Parameters.Add(CreateOpenApiParameter(attribute.PropertyId, ParameterLocation.Path, description));
    }

    private static OpenApiResponse CreateResponseItem(Type? dtoType, DocumentFilterContext context)
    {
        var schema = dtoType is null
            ? new OpenApiSchema()
            : context.SchemaGenerator.GenerateSchema(dtoType, context.SchemaRepository);
        return new OpenApiResponse { Content = { ["application/json"] = new OpenApiMediaType { Schema = schema } } };
    }

    private static OpenApiParameter CreateOpenApiParameter(string propertyId, ParameterLocation location, string description) =>
        new()
        {
            Name = propertyId,
            In = location,
            Schema = new OpenApiSchema { Type = "string" },
            Description = description
        };

     private static bool IsCommandHandlerInterface(Type type) => IsTraditionalCommandHandlerInterface(type) || IsCommandlessHandlerInterface(type);

     private static bool IsCommandlessHandlerInterface(Type interfaceType) =>
        interfaceType.IsGenericType
            ? interfaceType.GetGenericTypeDefinition() == typeof(ICommandlessHandler<>)
            : interfaceType == typeof(ICommandlessHandler);

    private static bool IsTraditionalCommandHandlerInterface(Type interfaceType)
    {
        if (!interfaceType.IsGenericType) return false;
        var genericBaseType = interfaceType.GetGenericTypeDefinition();
        return genericBaseType == typeof(ICommandHandler<>) ||
        genericBaseType == typeof(ICommandHandler<,>);
    }

    private readonly struct CommandHandlerType
    {
        public Type Type { get; init; }
        public ModifiesEntityAttribute CommandAttribute { get; init; }
    }
}
