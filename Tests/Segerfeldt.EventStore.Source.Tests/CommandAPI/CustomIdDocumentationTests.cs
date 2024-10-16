using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

using Segerfeldt.EventStore.Source.CommandAPI;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Tests.CommandAPI;

public sealed class CustomIdDocumentationTests
{
    [Test]
    public void TestGeneratorWorksLikeTheRealOne()
    {
        var context = TestableDocumentFilterContext();
        Assert.That(context.SchemaGenerator, Is.InstanceOf<TestGenerator>()); ;

        var referenceSchema = context.SchemaGenerator.GenerateSchema(typeof(CommandlessHandler), context.SchemaRepository);
        Assert.That(referenceSchema, Is.Not.Null);
        Assert.That(referenceSchema.Reference.Id, Is.EqualTo(typeof(CommandlessHandler).FullName));
        Assert.That(referenceSchema.Reference.Type, Is.EqualTo(ReferenceType.Schema));
        Assert.That(referenceSchema.Description, Is.Null);

        Assert.That(context.SchemaRepository.Schemas.TryGetValue(typeof(CommandlessHandler).FullName!, out var fullSchema), Is.True);
        Assert.That(fullSchema, Is.Not.Null);
        Assert.That(fullSchema?.Reference, Is.Null);
        Assert.That(fullSchema?.Description, Is.EqualTo("Summary for [CommandlessHandler]"));
    }

    private OpenApiDocument document = null!;
    private DocumentationGenerator generator = null!;

    [SetUp]
    public void SetUp()
    {
        var context = TestableDocumentFilterContext();
        document = new OpenApiDocument
        {
            Components = new OpenApiComponents { Schemas = context.SchemaRepository.Schemas },
            Paths = new(),
        };
        generator = new DocumentationGenerator(context);
    }

    private static DocumentFilterContext TestableDocumentFilterContext() =>
        new(Array.Empty<ApiDescription>(), new TestGenerator() { SchemaIdFunc = type => type.FullName! }, new SchemaRepository());

    [TestCase(typeof(CommandHandler), typeof(Command))]
    [TestCase(typeof(CommandHandlerWithDTO), typeof(Command))]
    [TestCase(typeof(CommandlessHandler), typeof(CommandlessHandler))]
    [TestCase(typeof(CommandlessHandlerWithDTO), typeof(CommandlessHandlerWithDTO))]
    public void DocumentsCommandHandler(Type commandHandlerType, Type schemaType)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();
        Assert.That(GetCommandSchema(schemaType.FullName!).Description, Is.EqualTo($"Summary for [{schemaType.Name}]"));
    }

    [TestCase(typeof(CommandHandler))]
    [TestCase(typeof(CommandHandlerWithDTO))]
    public void DocumentsRequestBodyParameters(Type commandHandlerType)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();

        var property = GetPropertySchema(typeof(Command).FullName!, "parameter");
        Assert.That(property.Description, Is.EqualTo("Summary for [Command.Parameter]"));
    }

    [TestCase(typeof(CommandHandler))]
    [TestCase(typeof(CommandHandlerWithDTO))]
    public void DocumentsRequestBody(Type commandHandlerType)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();

        var requestBody = GetOperation(OperationType.Post, "/entity").RequestBody;
        Assert.That(requestBody.Content, Does.ContainKey("application/json"));
        var schema = requestBody.Content["application/json"].Schema;
        Assert.Multiple(() =>
        {
            Assert.That(schema.Reference.Id, Is.EqualTo("Command"));
            Assert.That(schema.Reference.Type, Is.EqualTo(ReferenceType.Schema));
        });
    }

    [TestCase(typeof(CommandHandlerWithDTO))]
    [TestCase(typeof(CommandlessHandlerWithDTO))]
    public void DocumentsResponse(Type commandHandlerType)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();
        Assert.That(GetCommandSchema(typeof(Result).FullName!).Description, Is.EqualTo("Summary for [Result]"));
    }

    [TestCase(typeof(CommandHandlerWithDTO))]
    [TestCase(typeof(CommandlessHandlerWithDTO))]
    public void DocumentsResponseProperties(Type commandHandlerType)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();

        var schema = GetPropertySchema(typeof(Result).FullName!, "value");
        Assert.That(schema.Description, Is.EqualTo("Summary for [Result.Value]"));
    }

    [TestCase(typeof(CommandHandlerWithDTO), OperationType.Post, "/entity")]
    [TestCase(typeof(CommandlessHandlerWithDTO), OperationType.Delete, "/entity/{id1}/property/{id2}")]
    public void DocumentsResponseBody(Type commandHandlerType, OperationType method, string pattern)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();

        var schema = GetResponseContent(method, pattern, HttpStatusCode.OK, "application/json");
        Assert.Multiple(() =>
        {
            Assert.That(schema.Type, Is.EqualTo("Result"));
            // TODO: This is generated by the mock. Meaningless to test?
            Assert.That(schema.Reference.Id, Is.EqualTo(typeof(Result).FullName));
            Assert.That(schema.Reference.Type, Is.EqualTo(ReferenceType.Schema));
        });
    }

    [TestCase(typeof(CommandHandler), OperationType.Post, "/entity")]
    [TestCase(typeof(CommandlessHandler), OperationType.Delete, "/entity/{id1}/property/{id2}")]
    public void DocumentsResponseForHandlerWithoutDTO(Type commandHandlerType, OperationType method, string pattern)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();

        var response = GetResponse(method, pattern, HttpStatusCode.NoContent);
        Assert.That(response.Content, Is.Null.Or.Empty);
    }

    [TestCase(typeof(CommandHandler), "Command", OperationType.Post, "/entity", "POST /entity")]
    [TestCase(typeof(CommandHandlerWithDTO), "Command", OperationType.Post, "/entity", "POST /entity")]
    [TestCase(typeof(CommandlessHandler), "CommandlessHandler", OperationType.Delete, "/entity/{id1}/property/{id2}", "DELETE /entity/{id1}/property/{id2}")]
    [TestCase(typeof(CommandlessHandlerWithDTO), "CommandlessHandlerWithDTO", OperationType.Delete, "/entity/{id1}/property/{id2}", "DELETE /entity/{id1}/property/{id2}")]
    public void DocumentsOperations(Type commandHandlerType, string name, OperationType method, string pattern, string operationId)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();

        var operation = GetOperation(method, pattern);
        Assert.Multiple(() =>
        {
            Assert.That(operation.OperationId, Is.EqualTo(operationId));
            Assert.That(operation.Tags.Select(t => t.Name), Is.EquivalentTo(new[] { "Entity" }));
            Assert.That(operation.Summary, Is.EqualTo($"Summary for [{name}]"));
        });
    }

    [TestCase(typeof(CommandlessHandler))]
    [TestCase(typeof(CommandlessHandlerWithDTO))]
    public void DocumentsParameters(Type commandHandlerType)
    {
        GivenCommandHandler(commandHandlerType);
        WhenGeneratingDocs();

        var parameters = GetOperation(OperationType.Delete, "/entity/{id1}/property/{id2}").Parameters;
        Assert.Multiple(() =>
        {
            Assert.That(parameters, Has.Count.EqualTo(2));
            Assert.That(parameters[0].Name, Is.EqualTo("id1"));
            Assert.That(parameters[0].Description, Is.EqualTo("the entity id of the Entity to modify"));
            Assert.That(parameters[0].Schema.Type, Is.EqualTo("string"));
            Assert.That(parameters[0].In, Is.EqualTo(ParameterLocation.Path));
            Assert.That(parameters[1].Name, Is.EqualTo("id2"));
            Assert.That(parameters[1].Description, Is.EqualTo("the entity id of the property to remove"));
            Assert.That(parameters[1].Schema.Type, Is.EqualTo("string"));
            Assert.That(parameters[1].In, Is.EqualTo(ParameterLocation.Path));
        });
    }

    [Test]
    public void AllowsOverloadingPatternWithDifferentMethod()
    {
        GivenCommandHandler(typeof(CommandHandler));
        GivenCommandHandler(typeof(OverloadingCommandHandler));
        WhenGeneratingDocs();

        Assert.That(document.Paths, Does.ContainKey("/entity"));
        Assert.That(document.Paths["/entity"].Operations, Does.ContainKey(OperationType.Post));
        Assert.That(document.Paths["/entity"].Operations, Does.ContainKey(OperationType.Delete));
    }

    private OpenApiSchema GetResponseContent(OperationType method, string pattern, HttpStatusCode statusCode, string mimeType)
    {
        var response = GetResponse(method, pattern, statusCode);
        Assert.That(response.Content, Does.ContainKey(mimeType));
        return response.Content[mimeType].Schema;
    }

    private OpenApiResponse GetResponse(OperationType post, string pattern, HttpStatusCode statusCode)
    {
        var statusCodeString = $"{(int)statusCode}";
        var responses = GetOperation(post, pattern).Responses;
        Assert.That(responses, Does.ContainKey(statusCodeString));
        return responses[statusCodeString];
    }

    private OpenApiOperation GetOperation(OperationType method, string pattern)
    {
        Assert.That(document.Paths, Does.ContainKey(pattern));
        var pathItem = document.Paths[pattern];
        Assert.That(pathItem.Operations, Does.ContainKey(method));
        return pathItem.Operations[method];
    }

    private OpenApiSchema GetPropertySchema(string commandName, string propertyName)
    {
        var properties = GetCommandSchema(commandName).Properties;
        Assert.That(properties, Does.ContainKey(propertyName));
        return properties[propertyName];
    }

    private void GivenCommandHandler(Type type)
    {
        generator.AddCommandHandler(type);
    }

    private void WhenGeneratingDocs()
    {
        generator.Generate(document);
    }

    private OpenApiSchema GetCommandSchema(string name)
    {
        var schemas = document.Components.Schemas;
        Assert.That(schemas, Does.ContainKey(name));
        return schemas[name];
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
