using Microsoft.Extensions.Hosting;

using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.Hosting;

internal sealed class HostedEventSource(EventSource eventSource) : IHostedService
{
    private readonly EventSource eventSource = eventSource;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventSource.BeginProjecting();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
