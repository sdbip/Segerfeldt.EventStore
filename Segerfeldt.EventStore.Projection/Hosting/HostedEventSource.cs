using Microsoft.Extensions.Hosting;

using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.Hosting
{
    internal sealed class HostedEventSource : IHostedService
    {
        private readonly EventSource eventSource;

        public HostedEventSource(EventSource eventSource)
        {
            this.eventSource = eventSource;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            eventSource.StartReceiving();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
