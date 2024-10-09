using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Source.CommandAPI;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class CommandsDocumentFilter(IEnumerable<Assembly> assemblies) : IDocumentFilter
{
    private readonly IEnumerable<Assembly> assemblies = assemblies;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var generator = new DocumentationGenerator(context);
        var types = FindCommandHandlerTypes();
        foreach (var type in types)
            generator.AddCommandHandler(type);
        generator.Generate(swaggerDoc);
    }

    private IEnumerable<Type> FindCommandHandlerTypes() =>
        assemblies
            .SelectMany(assembly => assembly.DefinedTypes
                .Where(type => type.IsClass && !type.IsAbstract &&
                    type.GetCustomAttribute<ModifiesEntityAttribute>(false) is not null));
}
