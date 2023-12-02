using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Segerfeldt.EventStore.Projection.Hosting;

using System.Linq;

using MS = Microsoft.AspNetCore.Mvc.Testing;

namespace Segerfeldt.EventStore.Projection.NUnit;

public class WebApplicationFactory<TStartup> : MS.WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            var eventSources = (EventSources)services.First(s => s.ServiceType == typeof(EventSources)).ImplementationInstance!;

            services.AddSingleton(provider =>
            {
                // TODO: This is weird and ugly, but it appears to be the only
                // way to access the `IServiceProvider` and assign it to the
                // `EventSources` singleton.
                eventSources.SetProvider(provider);
                return eventSources;
            });

            ConfigureServices(services);
        });
        base.ConfigureWebHost(builder);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
