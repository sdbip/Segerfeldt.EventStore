using JetBrains.Annotations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public abstract class ProjectorBase : IProjector
    {
        private readonly Lazy<Dictionary<string, IEnumerable<MethodInfo>>> lazyMethods;

        public IEnumerable<string> HandledEvents => lazyMethods.Value.Keys;

        protected ProjectorBase()
        {
            lazyMethods = new Lazy<Dictionary<string, IEnumerable<MethodInfo>>>(
                () => GetPublicInstanceMethods()
                    .Select(m => (method: m, attribute: m.GetCustomAttribute<ProjectsEventAttribute>()))
                    .Where(ma => ma.attribute is not null)
                    .Select(ma => (ma.method, ma.attribute!))
                    .GroupBy(ma => ma.Item2.Event)
                    .ToDictionary(g => g.Key, g => g.Select(ma => ma.method)));
        }

        public async Task InvokeAsync(Event @event)
        {
            if (!lazyMethods.Value.TryGetValue(@event.Name, out var methods)) return;

            var tasks = methods
                .Select(m => InvokeMethod(m, @event))
                .OfType<Task>();
            await Task.WhenAll(tasks);
        }

        private object? InvokeMethod(MethodBase method, Event @event)
        {
            var parameters = method.GetParameters();
            var arguments = parameters.Length == 2
                ? new[] {@event.EntityId, @event.DetailsAs(parameters[1].ParameterType)}
                : new object?[]{@event};
            return method.Invoke(this, arguments);
        }

        private IEnumerable<MethodInfo> GetPublicInstanceMethods() => GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

        [AttributeUsage(AttributeTargets.Method)]
        [MeansImplicitUse]
        protected class ProjectsEventAttribute : Attribute
        {
            public string Event { get; }

            public ProjectsEventAttribute(string @event)
            {
                Event = @event;
            }
        }
    }
}
