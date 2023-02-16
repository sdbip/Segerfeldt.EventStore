using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Segerfeldt.EventStore.Projection.Hosting;
using MS = Microsoft.AspNetCore.Mvc.Testing;

namespace Segerfeldt.EventStore.Projection.NUnit;

public class WebApplicationFactory<TStartup> : MS.WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly EventSources eventSources = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ServiceCollectionExtension.BuilderCreated += (sourceBuilder, eventSourceName) =>
        {
            eventSources.Add(sourceBuilder, eventSourceName);
        };

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddSingleton(new EventSources());
            services.AddSingleton(provider =>
            {
                eventSources.SetProvider(provider);
                return eventSources;
            });
        });
        base.ConfigureWebHost(builder);
    }
}
