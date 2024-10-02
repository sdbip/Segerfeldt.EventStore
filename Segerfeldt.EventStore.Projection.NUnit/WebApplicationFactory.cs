using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MS = Microsoft.AspNetCore.Mvc.Testing;

namespace Segerfeldt.EventStore.Projection.NUnit;

public class WebApplicationFactory<TStartup> : MS.WebApplicationFactory<TStartup> where TStartup : class
{
    protected override sealed void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove services to avoid starting them. (But they still need to be instantiated.)
            var hosteds = new List<ServiceDescriptor>(services.Where(x => x.ServiceType == typeof(IHostedService)));
            services.RemoveAll<IHostedService>();
            services.AddSingleton<IHostedService>(provider =>
            {
                // Trigger the setups for the hosted services, but don't start them.
                foreach (var hosted in hosteds) hosted.ImplementationFactory?.Invoke(provider);
                return new InsipidService();
            });

            ConfigureServices(services);
        });
        base.ConfigureWebHost(builder);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    private class InsipidService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
