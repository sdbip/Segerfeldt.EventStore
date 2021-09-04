using Microsoft.Extensions.DependencyInjection;

namespace Segerfeldt.EventStore.Projection.Hosting
{
    public static class ServiceCollectionExtension
    {
        public static EventSourceBuilder AddHostedEventSource(this IServiceCollection services, IConnectionPool connectionPool)
        {
            var builder = new EventSourceBuilder(connectionPool);
            services.AddHostedService(p => new HostedEventSource(builder.Build(p)));
            return builder;
        }
    }
}
