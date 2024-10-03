using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using MS = Microsoft.AspNetCore.Mvc.Testing;

namespace Segerfeldt.EventStore.Source.NUnit;

public class WebApplicationFactory<TStartup> : MS.WebApplicationFactory<TStartup> where TStartup : class
{
    protected override sealed void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove services to avoid starting them.
            services.RemoveAll<IHostedService>();

            ConfigureServices(services);
        });
        base.ConfigureWebHost(builder);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
