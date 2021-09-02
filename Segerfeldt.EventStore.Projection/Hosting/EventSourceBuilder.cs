using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Projection.Hosting
{
    public class EventSourceBuilder
    {
        private readonly List<Type> receptacleTypes = new();
        private Type? positionTrackerType;
        private readonly IConnectionPool connectionPool;

        public EventSourceBuilder(IConnectionPool connectionPool)
        {
            this.connectionPool = connectionPool;
        }

        public EventSourceBuilder AddReceptacles(Assembly assembly)
        {
            receptacleTypes.AddRange(assembly.ExportedTypes.Where(t => t.IsAssignableTo(typeof(IReceptacle))));
            return this;
        }

        public EventSourceBuilder SetPositionTracker<TPositionTracker>() where TPositionTracker : IPositionTracker
        {
            positionTrackerType = typeof(TPositionTracker);
            return this;
        }

        internal EventSource Build(IServiceProvider provider)
        {
            var eventSource = new EventSource(connectionPool, GetPositionTracker(provider));
            foreach (var type in receptacleTypes)
                eventSource.Register((IReceptacle)ActivatorUtilities.GetServiceOrCreateInstance(provider, type));

            return eventSource;
        }

        private IPositionTracker? GetPositionTracker(IServiceProvider provider) =>
            positionTrackerType is not null ? (IPositionTracker?)provider.GetService(positionTrackerType) : null;
    }
}
