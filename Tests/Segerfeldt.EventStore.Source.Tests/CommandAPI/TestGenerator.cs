using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Source.Tests.CommandAPI;

internal class TestGenerator : SimulatedGenerator
{
    protected override string PropertyDescription(Type modelType, PropertyInfo p) => $"Summary for [{modelType.Name}.{p.Name}]";
    protected override string TypeDescription(Type modelType) => $"Summary for [{modelType.Name}]";
}

// TODO: This simulation makes too many assumptions.
internal abstract class SimulatedGenerator : ISchemaGenerator
{
    public Func<Type, string> SchemaIdFunc { get; init; } = type => type.Name;

    public OpenApiSchema GenerateSchema(Type modelType, SchemaRepository schemaRepository, MemberInfo? memberInfo = null, ParameterInfo? parameterInfo = null, ApiParameterRouteInfo? routeInfo = null)
    {
        schemaRepository.RegisterType(modelType, SchemaIdFunc(modelType));
        schemaRepository.AddDefinition(SchemaIdFunc(modelType), new OpenApiSchema
        {
            Description = TypeDescription(modelType),
            Properties = modelType.GetProperties().ToDictionary(p => p.Name.ToLower(), p => new OpenApiSchema { Description = PropertyDescription(modelType, p) })
        });
        return new OpenApiSchema
        {
            Type = modelType.Name,
            Reference = new OpenApiReference { Id = SchemaIdFunc(modelType), Type = ReferenceType.Schema },
        };
    }

    protected abstract string PropertyDescription(Type modelType, PropertyInfo p);
    protected abstract string TypeDescription(Type modelType);
}
