using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Data;

namespace Segerfeldt.EventStore.Refactoring.Hosting;

[PublicAPI]
public static class ServiceCollectionExtension
{
    /// <summary>Set up an <see cref="EventSource"/> to project events for refactoring</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param name="sourceConnectionFactory">an function that creates connections to the write-model database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UseRefactoring(this IServiceCollection services, Func<IServiceProvider, IDbConnection> sourceConnectionFactory)
    {
        services.AddSingleton(p => new EventSourceRepository(sourceConnectionFactory(p)));
        services.AddSingleton<EventSource>();
        services.AddHostedService<HostedEventSource>();
        return new EventSourceConfiguration(services);
    }
}
