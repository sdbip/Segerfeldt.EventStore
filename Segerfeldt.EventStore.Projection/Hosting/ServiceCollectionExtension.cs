using Microsoft.Extensions.DependencyInjection;

using System.Data;

namespace Segerfeldt.EventStore.Projection.Hosting
{
    public static class ServiceCollectionExtension
    {
        public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, IDbConnection connection)
        {
            var builder = new EventSourceBuilder(connection);
            services.AddHostedService(p => new HostedEventSource(builder.Build(p)));
            return builder;
        }
    }
}
