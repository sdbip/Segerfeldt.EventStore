using JetBrains.Annotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Source.CommandAPI.HTTPServices;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.CommandAPI;

public class EventStoreOptions
{
    public Action<IServiceProvider> PrepareDatabase { get; init; } = _ => {};
}

public class EventStoreConnectionFactory
{
    public required Func<DbConnection> FactoryFunc { get; init; }
    public static EventStoreConnectionFactory Singleton(DbConnection connection) => new() { FactoryFunc = () => connection};

    public DbConnection CreateConnection() => FactoryFunc.Invoke();
}

[PublicAPI]
public static class Commanding
{
    /// <summary>Add a custom EventStore write-model database</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param name="provider">an object that knows how to create connections to the write-model database</param>
    public static IServiceCollection UseEventStore(this IServiceCollection services, Func<IServiceProvider, DbConnection> connectionFunc, EventStoreOptions? options = null)
    {
        services.AddSingleton(p =>
        {
            options?.PrepareDatabase(p);
            return new EventStoreConnectionFactory { FactoryFunc = () => connectionFunc(p) };
        });
        return services;
    }

    public static ProjectionEndpointConfiguration UseProjectionEndpoint<TProjectionRepository>(this IServiceCollection services) where TProjectionRepository : class, IProjectionRepository
    {
        services.AddSingleton<IProjectionRepository, TProjectionRepository>();
        return services.UseProjectionEndpoint();
    }

    public static ProjectionEndpointConfiguration UseProjectionEndpoint(this IServiceCollection services)
    {
        var config = new ProjectionEndpointConfiguration(services);
        services.AddSingleton(config);
        return config;
    }

    /// <summary>Add Swagger documentation for command handlers from their XML documentation</summary>
    /// <param name="assemblies">assemblies to search for command definitions</param>
    /// <param name="swaggerOptions">the Swagger configuration to modify</param>
    public static SwaggerGenOptions DocumentCommands(this SwaggerGenOptions swaggerOptions, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0) assemblies = [Assembly.GetCallingAssembly()];

        swaggerOptions.DocumentFilter<HistoryDocumentFilter>();
        swaggerOptions.DocumentFilter<CommandsDocumentFilter>(assemblies.AsEnumerable());
        return swaggerOptions;
    }

    /// <summary>Add Swagger documentation for the projection/ endpoint</summary>
    /// <param name="swaggerOptions">the Swagger configuration to modify</param>
    public static SwaggerGenOptions DocumentProjectionEndpoint(this SwaggerGenOptions swaggerOptions)
    {
        swaggerOptions.DocumentFilter<ProjectionDocumentFilter>();
        return swaggerOptions;
    }

    /// <summary>Map endpoints to command handlers</summary>
    /// <param name="builder">the web-app configuration to modify</param>
    /// <param name="assemblies">assemblies to search for command definitions</param>
    public static IEndpointRouteBuilder MapCommands(this IEndpointRouteBuilder builder, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0) assemblies = [Assembly.GetCallingAssembly()];

        builder.MapHistory();
        if (builder.ServiceProvider.GetService<ProjectionEndpointConfiguration>() is not null)
            builder.MapProjectionEndpoint();
        builder.MapCommandHandlers(assemblies);
        return builder;
    }

    private static void MapProjectionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("history/", GetProjection);
    }

    private static async Task GetProjection(HttpContext context)
    {
        var result = await new ProjectionQueryRequest(context).GetAsync();
        await SendResponse(context, result);
    }

    private static void MapHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("history/{entityId}", GetHistory).WithGroupName("History");
    }

    private static async Task GetHistory(HttpContext context, string entityId)
    {
        var result = await new HistoryQueryRequest(context).Get();
        await SendResponse(context, result);
    }

    private static void MapCommandHandlers(this IEndpointRouteBuilder endpoints, Assembly[] assemblies)
    {
        var attributedClasses = assemblies
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetCustomAttribute<ModifiesEntityAttribute>(false) is not null)
            .ToList();


        if (attributedClasses.Any(type => !HasCommandHandlerInterface(type)))
            throw new NotSupportedException($"Invalid class(es): {string.Join(',',attributedClasses.Select(type => type.FullName))}. The {nameof(ModifiesEntityAttribute)} requires implementating ICommandHandler<>");

        foreach (var handlerClass in attributedClasses)
        {
            var attribute = handlerClass.GetCustomAttribute<ModifiesEntityAttribute>()!;
            endpoints.MapMethods(attribute.Pattern, [attribute.MethodString], context => HandleCommand(context, handlerClass));
        }
    }

    private static bool HasCommandHandlerInterface(TypeInfo info)
    {
        var interfaces = info.GetInterfaces().Where(i => i.IsGenericType);
        if (interfaces.Any(t => t.GetGenericTypeDefinition() == typeof(ICommandHandler<>) || t.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))) return true;
        if (info.BaseType is null) return false;
        return HasCommandHandlerInterface(info.BaseType.GetTypeInfo());
    }

    private static async Task HandleCommand(HttpContext context, TypeInfo handlerClass)
    {
        var result = await new CommandInputRequest(handlerClass, context).Execute();
        await SendResponse(context, result);
    }

    private static async Task SendResponse(HttpContext context, ActionResult response)
    {
        await response.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
    }
}
