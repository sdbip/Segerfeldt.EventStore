using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Segerfeldt.EventStore.Source.CommandAPI;

using MS = Microsoft.AspNetCore.Mvc.Testing;

namespace Segerfeldt.EventStore.Source.NUnit;

/// <inheritdoc/>
public class WebApplicationFactory<TEntryPoint> : MS.WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    protected sealed override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove services to avoid starting them.
            services.RemoveAll<IHostedService>();

            ConfigureServices(services);
        });
        base.ConfigureWebHost(builder);
    }

    /// <summary>Add optional services used for testing</summary>
    /// <param name="services">The Web API service configuration</param>
    protected virtual void ConfigureServices(IServiceCollection services) { }

    public void ClearSourceTables()
    {
        var connectionFactory = Services.GetRequiredService<EventStoreConnectionFactory>();
        var connection = connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Events; DELETE FROM Entities";

        connection.Open();
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }
    }
}
