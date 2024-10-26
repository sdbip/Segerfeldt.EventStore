using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using System.Data;
using System.Linq;

namespace Segerfeldt.EventStore.Refactoring.Hosting;

[PublicAPI]
public static class ServiceCollectionExtension
{
    /// <summary>Add an <see cref="EventSource"/> to project events</summary>
    /// <param name="services">the Web API builder services</param>
    /// <param name="provider">an object that knows how to create connections to the write-model database</param>
    /// <returns>An <see cref="EventSourceConfiguration"/> for allowing additional configuration</returns>
    public static EventSourceConfiguration UseRefactoring(this IServiceCollection services, Func<IServiceProvider, IDbConnection> sourceConnection)
    {
        services.AddSingleton(p => new EventSourceRepository(sourceConnection(p)));
        services.AddSingleton<EventSource>();
        services.AddHostedService<HostedEventSource>();
        return new EventSourceConfiguration(services);
    }
}
