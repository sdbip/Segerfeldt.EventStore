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
        private readonly IDbConnection connection;
        private readonly List<Type> projectionTypes = new();

        public EventSourceBuilder(IDbConnection connection)
        {
            this.connection = connection;
        }

        public void AddProjections()
        {
            AddProjections(Assembly.GetEntryAssembly()!);
        }

        public void AddProjections(Assembly assembly)
        {
            projectionTypes.AddRange(assembly.ExportedTypes.Where(t => t.IsAssignableTo(typeof(IProjector))));
        }

        internal EventSource Build(IServiceProvider provider)
        {
            var eventSource = new EventSource(connection);
            foreach (var type in projectionTypes)
                eventSource.AddProjection((IProjector)ActivatorUtilities.GetServiceOrCreateInstance(provider, type));
            return eventSource;
        }
    }
}
