using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public abstract class ProjectionBase : IProjection
    {
        public IEnumerable<string> HandledEvents => GetPublicInstanceMethods()
            .Select(m => m.GetCustomAttribute<ProjectsEventAttribute>()?.Event)
            .OfType<string>();

        public async Task InvokeAsync(Event @event)
        {
            var methods = FindProjectionMethods(@event);
            foreach (var method in methods)
                await InvokeProjectionMethod(method, @event);
        }

        private IEnumerable<MethodInfo> FindProjectionMethods(Event @event) => GetPublicInstanceMethods()
            .Where(m => m.GetCustomAttribute<ProjectsEventAttribute>()?.Event == @event.Name);

        private IEnumerable<MethodInfo> GetPublicInstanceMethods() => GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

        private async Task InvokeProjectionMethod(MethodBase method, Event @event)
        {
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToImmutableList();
            var args = parameterTypes[0] == typeof(Event)
                ? new object?[] { @event }
                : new[] { @event.EntityId, @event.DetailsAs(parameterTypes[1]) };
            if (method.Invoke(this, args) is Task task) await task;
        }
    }
}
