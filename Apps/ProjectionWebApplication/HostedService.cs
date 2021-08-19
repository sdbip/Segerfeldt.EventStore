using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Segerfeldt.EventStore.Projection;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectionWebApplication
{
    public class HostedService : IHostedService
    {
        private readonly EventSource eventSource;
        private readonly IServiceProvider provider;

        public HostedService(EventSource eventSource, IServiceProvider provider)
        {
            this.eventSource = eventSource;
            this.provider = provider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            eventSource.AddProjection(provider.GetService<ScoreBoard>()!);
            eventSource.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
