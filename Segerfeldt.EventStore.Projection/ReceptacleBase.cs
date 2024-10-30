using JetBrains.Annotations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection;

/// <summary>
/// Base class for creating receptacles without having to explicitly implement all
/// of the <see cref="IReceptacle"/> interface.
/// Receptacles are the tools of projection.
/// </summary>
public abstract class ReceptacleBase : IReceptacle
{
    private readonly Lazy<Dictionary<string, IEnumerable<MethodInfo>>> lazyMethods;

    /// <inheritdoc/>
    public IEnumerable<string> AcceptedEvents => lazyMethods.Value.Keys;

    protected Transaction Transaction { get; private set; } = null!;

    protected ReceptacleBase()
    {
        lazyMethods = new Lazy<Dictionary<string, IEnumerable<MethodInfo>>>(
            () => GetPublicInstanceMethods()
                .Select(m => (method: m, attribute: m.GetCustomAttribute<ReceivesEventAttribute>()))
                .Where(ma => ma.attribute is not null)
                .Select(ma => (ma.method, attribute: ma.attribute!))
                .GroupBy(ma => ma.attribute.Name)
                .ToDictionary(g => g.Key, g => g.Select(ma => ma.method)));
    }

    /// <inheritdoc/>
    public void Update(Event @event, Transaction transaction)
    {
        Transaction = transaction;
        if (!lazyMethods.Value.TryGetValue(@event.Name, out var methods)) return;

        foreach (var method in methods.Where(m => m.GetCustomAttribute<ReceivesEventAttribute>()!.Accepts(@event)))
            if (InvokeMethod(method, @event) is Task task) task.Wait();
    }

    private object? InvokeMethod(MethodBase method, Event @event)
    {
        var parameters = method.GetParameters();
        object?[] arguments = parameters.Length == 2
            ? [@event.EntityId, @event.DetailsAs(parameters[1].ParameterType)]
            : [@event];
        return method.Invoke(this, arguments);
    }

    private MethodInfo[] GetPublicInstanceMethods() => GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    protected class ReceivesEventAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
        public string? EntityType { get; init; }

        public bool Accepts(Event @event) => Name == @event.Name && (EntityType is null || EntityType == @event.EntityType);
    }
}
