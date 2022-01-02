using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Projection.Hosting
{
    public sealed class EventSourceBuilder
    {
        private readonly List<Func<IServiceProvider, IReceptacle>> receptacles = new();
        private Type? positionTrackerType;
        private readonly Func<IServiceProvider, IConnectionPool> getConnectionPool;

        public EventSourceBuilder(Func<IServiceProvider, IConnectionPool> getConnectionPool)
        {
            this.getConnectionPool = getConnectionPool;
        }

        public EventSourceBuilder AddReceptacles(Assembly assembly)
        {
            var types = assembly.ExportedTypes.Where(t => t.IsAssignableTo(typeof(IReceptacle)));
            foreach (var type in types)
                receptacles.Add(provider => (IReceptacle)ActivatorUtilities.GetServiceOrCreateInstance(provider, type));
            return this;
        }

        public EventSourceBuilder SetPositionTracker<TPositionTracker>() where TPositionTracker : IPositionTracker
        {
            positionTrackerType = typeof(TPositionTracker);
            return this;
        }

        internal EventSource Build(IServiceProvider provider)
        {
            var eventSource = new EventSource(getConnectionPool(provider), GetPositionTracker(provider));
            foreach (var receptacle in receptacles)
                eventSource.Register(receptacle(provider));

            return eventSource;
        }

        private IPositionTracker? GetPositionTracker(IServiceProvider provider) =>
            positionTrackerType is not null ? (IPositionTracker?)ActivatorUtilities.GetServiceOrCreateInstance(provider, positionTrackerType) : null;

        public EventSourceBuilder AddReceptacle<TReceptacle>() where TReceptacle : IReceptacle
        {
            receptacles.Add(provider => ActivatorUtilities.GetServiceOrCreateInstance<TReceptacle>(provider));
            return this;
        }
    }
}
