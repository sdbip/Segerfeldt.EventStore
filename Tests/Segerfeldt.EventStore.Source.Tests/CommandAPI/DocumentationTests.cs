using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

using NUnit.Framework;
using NUnit.Framework.Constraints;

using Segerfeldt.EventStore.Source.CommandAPI;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Tests.CommandAPI;

public sealed class DocumentationTests
{
    [Test]
    public void TestGeneratorWorksLikeTheRealOne()
    {
        var context = new DocumentFilterContext(Array.Empty<ApiDescription>(), new TestGenerator(), new SchemaRepository("This is the one"));

        var referenceSchema = context.SchemaGenerator.GenerateSchema(typeof(CommandlessHandler), context.SchemaRepository);
        Assert.That(referenceSchema, Is.Not.Null);
        Assert.That(referenceSchema.Reference.Id, Is.EqualTo(nameof(CommandlessHandler)));
        Assert.That(referenceSchema.Description, Is.Null);

        Assert.That(context.SchemaRepository.Schemas.TryGetValue(nameof(CommandlessHandler), out var fullSchema), Is.True);
        Assert.That(fullSchema, Is.Not.Null);
        Assert.That(fullSchema?.Reference, Is.Null);
        Assert.That(fullSchema?.Description, Is.EqualTo("Summary for [CommandlessHandler]"));
    }

    private DocumentFilterContext context = null!;
    private OpenApiDocument document = null!;

    [SetUp]
    public void SetUp()
    {
        context = new DocumentFilterContext(Array.Empty<ApiDescription>(), new TestGenerator(), new SchemaRepository());
        document = new OpenApiDocument
        {
            Components = new OpenApiComponents { Schemas = context.SchemaRepository.Schemas },
            Paths = new(),
        };
    }

    [Test]
    public void DocumentsCommandHandler()
    {
        var generator = new DocumentationGenerator(context);

        generator.AddCommandHandler(typeof(CommandHandler));

        generator.Generate(document);

        Assert.That(document.Components.Schemas, Does.ContainKey(nameof(Command)));
        Assert.That(document.Components.Schemas[nameof(Command)].Description, Is.EqualTo("Summary for [Command]"));
        Assert.That(document.Components.Schemas[nameof(Command)].Properties, Is.Not.Null);
        Assert.That(document.Components.Schemas[nameof(Command)].Properties, Does.ContainKey("parameter"));
        Assert.That(document.Components.Schemas[nameof(Command)].Properties["parameter"].Description, Is.EqualTo("Summary for [Command.Parameter]"));
    }

    [Test]
    public void DocumentsOperationsWithoutDTO_0()
    {
        var generator = new DocumentationGenerator(context);

        generator.AddCommandHandler(typeof(CommandHandler));

        generator.Generate(document);

        Assert.That(document.Paths, Does.ContainKey("/entity"));
        Assert.That(document.Paths["/entity"].Operations, Does.ContainKey(OperationType.Post));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].OperationId, Is.EqualTo("POST /entity"));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Responses, Does.ContainKey("204"));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Responses["204"].Content, Is.Null.Or.Empty);
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Tags.Select(t => t.Name), Is.EquivalentTo(new [] { "Entity" }));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Summary, Is.EqualTo("Summary for [Command]"));
    }

    [Test]
    public void DocumentsResponseDTO_0()
    {
        var generator = new DocumentationGenerator(context);

        var handler = typeof(CommandHandlerWithDTO);
        generator.AddCommandHandler(handler);

        generator.Generate(document);

        Assert.That(document.Components.Schemas[nameof(Result)].Description, Is.EqualTo("Summary for [Result]"));
        Assert.That(document.Components.Schemas[nameof(Result)].Properties, Does.ContainKey("value"));
        Assert.That(document.Components.Schemas[nameof(Result)].Properties["value"].Description, Is.EqualTo("Summary for [Result.Value]"));
    }

    [Test]
    public void DocumentsOperationsWithRequestBody()
    {
        var generator = new DocumentationGenerator(context);

        var handler = typeof(CommandHandlerWithDTO);
        generator.AddCommandHandler(handler);

        generator.Generate(document);

        Assert.That(document.Components.Schemas[nameof(Command)].Description, Is.EqualTo("Summary for [Command]"));
        Assert.That(document.Components.Schemas[nameof(Command)].Properties, Does.ContainKey("parameter"));
        Assert.That(document.Components.Schemas[nameof(Command)].Properties["parameter"].Description, Is.EqualTo("Summary for [Command.Parameter]"));

        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].RequestBody, Is.Not.Null);
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].RequestBody.Content, Does.ContainKey("application/json"));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].RequestBody.Content["application/json"].Schema, Is.Not.Null);
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].RequestBody.Content["application/json"].Schema.Reference, Is.Not.Null);
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].RequestBody.Content["application/json"].Schema.Reference.Id, Is.EqualTo("Command"));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].RequestBody.Content["application/json"].Schema.Reference.Type, Is.EqualTo(ReferenceType.Schema));
    }

    [Test]
    public void DocumentsOperationsWithResponseDTO_0()
    {
        var generator = new DocumentationGenerator(context);

        var handler = typeof(CommandHandlerWithDTO);
        generator.AddCommandHandler(handler);

        generator.Generate(document);

        Assert.That(document.Paths, Does.ContainKey("/entity"));
        Assert.That(document.Paths["/entity"].Operations, Does.ContainKey(OperationType.Post));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].OperationId, Is.EqualTo("POST /entity"));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Responses, Does.ContainKey("200"));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Responses["200"].Content, Does.ContainKey("application/json"));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Responses["200"].Content["application/json"].Schema, Is.Not.Null);
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Responses["200"].Content["application/json"].Schema.Type, Is.EqualTo("Result"));
        Assert.That(document.Paths["/entity"].Operations[OperationType.Post].Responses["200"].Content["application/json"].Schema.Reference.Id, Is.EqualTo("Result"));
    }

    [Test]
    public void DocumentsCommandlessHandler()
    {
        var generator = new DocumentationGenerator(context);

        var handler = typeof(CommandlessHandler);
        generator.AddCommandHandler(handler);

        generator.Generate(document);

        Assert.That(document.Components.Schemas[nameof(CommandlessHandler)].Description, Is.EqualTo("Summary for [CommandlessHandler]"));
    }

    [Test]
    public void DocumentsOperationsWithoutDTO()
    {
        var generator = new DocumentationGenerator(context);

        var handler = typeof(CommandlessHandler);
        generator.AddCommandHandler(handler);

        generator.Generate(document);

        Assert.That(document.Paths, Does.ContainKey("/entity/{id1}/property/{id2}"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations, Does.ContainKey(OperationType.Delete));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].OperationId, Is.EqualTo("DELETE /entity/{id1}/property/{id2}"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters, Has.Count.EqualTo(2));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters[0].Name, Is.EqualTo("id1"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters[0].Description, Is.EqualTo("the entity id of the Entity to modify"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters[0].Schema.Type, Is.EqualTo("string"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters[0].In, Is.EqualTo(ParameterLocation.Path));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters[1].Name, Is.EqualTo("id2"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters[1].Description, Is.EqualTo("the entity id of the property to remove"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters[1].Schema.Type, Is.EqualTo("string"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Parameters[1].In, Is.EqualTo(ParameterLocation.Path));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Responses, Does.ContainKey("204"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Responses["204"].Content, Is.Null.Or.Empty);
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Tags.Select(t => t.Name), Is.EquivalentTo(new [] { "Entity" }));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Summary, Is.EqualTo("Summary for [CommandlessHandler]"));
    }

    [Test]
    public void DocumentsResponseDTO()
    {
        var generator = new DocumentationGenerator(context);

        var handler = typeof(CommandlessHandlerWithDTO);
        generator.AddCommandHandler(handler);

        generator.Generate(document);

        Assert.That(document.Components.Schemas[nameof(Result)].Description, Is.EqualTo("Summary for [Result]"));
        Assert.That(document.Components.Schemas[nameof(Result)].Properties, Does.ContainKey("value"));
        Assert.That(document.Components.Schemas[nameof(Result)].Properties["value"].Description, Is.EqualTo("Summary for [Result.Value]"));
    }

    [Test]
    public void DocumentsOperationsWithResponseDTO()
    {
        var generator = new DocumentationGenerator(context);

        var handler = typeof(CommandlessHandlerWithDTO);
        generator.AddCommandHandler(handler);

        generator.Generate(document);

        Assert.That(document.Paths, Does.ContainKey("/entity/{id1}/property/{id2}"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations, Does.ContainKey(OperationType.Delete));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].OperationId, Is.EqualTo("DELETE /entity/{id1}/property/{id2}"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Responses, Does.ContainKey("200"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Responses["200"].Content, Does.ContainKey("application/json"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Responses["200"].Content["application/json"].Schema, Is.Not.Null);
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Responses["200"].Content["application/json"].Schema.Type, Is.EqualTo("Result"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Responses["200"].Content["application/json"].Schema.Reference.Id, Is.EqualTo("Result"));
        Assert.That(document.Paths["/entity/{id1}/property/{id2}"].Operations[OperationType.Delete].Tags.Select(t => t.Name), Is.EquivalentTo(new [] { "Entity" }));
    }

    [Test]
    public void AllowsOverloadingPatternWithDifferentMethod()
    {
        var generator = new DocumentationGenerator(context);

        generator.AddCommandHandler(typeof(CommandHandler));
        generator.AddCommandHandler(typeof(OverloadingCommandHandler));

        generator.Generate(document);

        Assert.That(document.Paths, Does.ContainKey("/entity"));
        Assert.That(document.Paths["/entity"].Operations, Does.ContainKey(OperationType.Post));
        Assert.That(document.Paths["/entity"].Operations, Does.ContainKey(OperationType.Delete));
    }

    [DeletesEntity("Entity", EntityId = "id1", Property = "property", PropertyId = "id2")]
    private class CommandlessHandler : ICommandlessHandler
    {
        public Task<CommandResult> Handle(CommandContext context)
        {
            throw new NotImplementedException();
        }
    }

    [DeletesEntity("Entity", EntityId = "id1", Property = "property", PropertyId = "id2")]
    private class CommandlessHandlerWithDTO : ICommandlessHandler<Result>
    {
        public Task<CommandResult<Result>> Handle(CommandContext context)
        {
            throw new NotImplementedException();
        }
    }

    [AddsEntity("Entity")]
    private class CommandHandler : ICommandHandler<Command>
    {
        public Task<CommandResult> Handle(Command command, CommandContext context)
        {
            throw new NotImplementedException();
        }
    }

    [ModifiesEntity("Entity", Method = OperationType.Delete)]
    private class OverloadingCommandHandler : ICommandHandler<OverloadingCommand>
    {
        public Task<CommandResult> Handle(OverloadingCommand command, CommandContext context)
        {
            throw new NotImplementedException();
        }
    }

    [AddsEntity("Entity")]
    private class CommandHandlerWithDTO : ICommandHandler<Command, Result>
    {
        public Task<CommandResult<Result>> Handle(Command command, CommandContext context)
        {
            throw new NotImplementedException();
        }
    }

    private record Command(string Parameter);
    private record OverloadingCommand(string Parameter);
    private record Result(string Value);
}


internal class TestGenerator : ISchemaGenerator
{
    public Func<Type, string?> TypeDescriptionFunc { get; set; } = type => $"Summary for [{type.Name}]";
    public Func<Type, PropertyInfo, string?> PropertyDescriptionFunc { get; set; } = (type, parameter) => $"Summary for [{type.Name}.{parameter.Name}]";

    public OpenApiSchema GenerateSchema(Type modelType, SchemaRepository schemaRepository, MemberInfo? memberInfo = null, ParameterInfo? parameterInfo = null, ApiParameterRouteInfo? routeInfo = null)
    {
        schemaRepository.RegisterType(modelType, modelType.Name);
        schemaRepository.AddDefinition(modelType.Name, new OpenApiSchema
        {
            Description = TypeDescriptionFunc.Invoke(modelType),
            Properties = modelType.GetProperties().ToDictionary(p => p.Name.ToLower(), p => new OpenApiSchema { Description = PropertyDescriptionFunc.Invoke(modelType, p) })
        });
        return new OpenApiSchema
        {
            Type = modelType.Name,
            Reference = new OpenApiReference { Id = modelType.Name },
        };
    }
}
