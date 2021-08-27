using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Projection.Hosting
{
    public class EventSourceBuilder
    {
        private readonly List<Type> projectionTypes = new();
        private Type? positionTrackerType;
        private readonly IDbConnection connection;

        public EventSourceBuilder(IDbConnection connection)
        {
            this.connection = connection;
        }

        public EventSourceBuilder AddProjections(Assembly assembly)
        {
            projectionTypes.AddRange(assembly.ExportedTypes.Where(t => t.IsAssignableTo(typeof(IProjector))));
            return this;
        }

        public EventSourceBuilder SetPositionTracker<TPositionTracker>() where TPositionTracker : IPositionTracker
        {
            positionTrackerType = typeof(TPositionTracker);
            return this;
        }

        internal EventSource Build(IServiceProvider provider)
        {
            var eventSource = new EventSource(connection, GetPositionTracker(provider));
            foreach (var type in projectionTypes)
                eventSource.Register((IProjector)ActivatorUtilities.GetServiceOrCreateInstance(provider, type));

            return eventSource;
        }

        private IPositionTracker? GetPositionTracker(IServiceProvider provider) =>
            positionTrackerType is not null ? (IPositionTracker?)provider.GetService(positionTrackerType) : null;
    }
}
