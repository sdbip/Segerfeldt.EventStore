using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.Hosting;

public class EventSourceOptions
{
    /// <summary>
    ///     An action to run after setup, just before projection starts
    ///     Can e.g. be used to prepare a schema for the target database.
    /// </summary>
    public Action<IServiceProvider> Initialization { get; set; } = _ => {};
}

[PublicAPI]
public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="baseURL">Base URL to the source application</param>
    /// <param name="options">Additional options</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfigurationWithoutTarget AddHostedEventSource(this IServiceCollection services, string name, Uri baseURL, EventSourceOptions? options = null) =>
        services.AddHostedEventSource(name, options, (p, n) => new WebServiceHistoryEventSourceRepository(baseURL));

    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param name="name">A unique name for the <see cref="EventSource"/></param>
    /// <param name="options">Additional options</param>
    /// <typeparam name="TEventSourceRepository">The type of the event source repository
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfigurationWithoutTarget AddHostedEventSource<TEventSourceRepository>(this IServiceCollection services, string name, EventSourceOptions? options = null)
            where TEventSourceRepository : class, IEventSourceRepository =>
        services.AddHostedEventSource(name, options,
            (p, n) => ActivatorUtilities.CreateInstance<TEventSourceRepository>(p, p.GetRequiredKeyedService<IDbConnection>(n)));

    private static EventSourceConfigurationWithoutTarget AddHostedEventSource<TEventSourceRepository>(this IServiceCollection services, string name, EventSourceOptions? options, Func<IServiceProvider, object, TEventSourceRepository> createNamedEventSourceRepository) where TEventSourceRepository : class, IEventSourceRepository
    {
        services.AddKeyedSingleton(name, (p, n) => new EventSource(
            createNamedEventSourceRepository(p, n),
            p.GetRequiredService<TargetDatabase>(),
            p.GetRequiredKeyedService<ReceptacleCollection>(n),
            p.GetKeyedService<IProjectionTracker>(n),
            p.GetKeyedService<IPollingStrategy>(n) ?? p.GetService<IPollingStrategy>()));

        // A new hosted service is created for each EventSource.

        // Note: AddHostedService<T>() will only add one service per unique type T. Even if called
        // multiple times. If the user needs to track more than one Source, we'd need a new
        // HostedEventSource *class* for each one. Fortunately, AddSingleton<IHostedService>() does
        // not have such restrictions. And all IHostedServices added *will* be started by the .Net
        // Web API system.

        services.AddSingleton<IHostedService>(p =>
        {
            options?.Initialization.Invoke(p);
            return new HostedEventSource(p.GetRequiredKeyedService<EventSource>(name));
        });

        return new EventSourceConfigurationWithoutTarget(services, name);
    }
}

internal class WebServiceHistoryEventSourceRepository(Uri baseURL) : IEventSourceRepository
{
    private readonly HttpClient client = new() { BaseAddress = baseURL };

    public IEnumerable<Event> GetEvents(long afterPosition, int maxCount)
    {
        var response = client.GetAsync($"/history?after={afterPosition}&maxCount={maxCount}").Result;
        var json = response.Content.ReadAsStringAsync().Result;
        return JsonSerializer.Deserialize<IEnumerable<Event>>(json) ?? [];
    }
}
