using JetBrains.Annotations;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Source;

/// <summary>
/// Base class for creating entities without having to explicitly implement all
/// of the <c cref="IEntity">IEntity</c> interface.
/// Entities are the carriers of system state.
/// <seealso cref="IEntity"/>
/// </summary>
/// <remarks>Initializes a new entity</remarks>
/// <param name="id">the unique identifier for this entity</param>
/// <param name="type">a type name that uniquely identifies the implementing class for compatibility checks</param>
/// <param name="version">the current version of a reconstituted entity, or <c cref="EntityVersion.New">New</c> for a not yet persisted entity</param>
public abstract class EntityBase(EntityId id, EntityType type, EntityVersion version) : IEntity
{
    private readonly List<UnpublishedEvent> unpublishedEvents = [];

    /// <inheritdoc/>
    public EntityId Id { get; } = id;
    /// <inheritdoc/>
    public EntityType Type { get; } = type;
    /// <inheritdoc/>
    public EntityVersion Version { get; } = version;
    /// <inheritdoc/>
    public IEnumerable<UnpublishedEvent> UnpublishedEvents => [.. unpublishedEvents];

    /// <summary>Adds a new event to the state</summary>
    /// <param name="event">the event to add</param>
    protected void Add(UnpublishedEvent @event)
    {
        unpublishedEvents.Add(@event);
    }

    /// <inheritdoc/>
    public virtual void ReplayEvents(IEnumerable<PublishedEvent> events)
    {
        foreach (var @event in events) ReplayEvent(@event);
    }

    private void ReplayEvent(PublishedEvent @event)
    {
        var methods = FindReplayMethods(@event);
        foreach (var method in methods)
            InvokeReplayMethod(method, @event);
    }

    private IEnumerable<MethodInfo> FindReplayMethods(PublishedEvent @event) =>
        GetPublicInstanceMethods().Where(ReplaysEvent(@event.Name));

    private IEnumerable<MethodInfo> GetPublicInstanceMethods() => GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.Instance);

    private static Func<MethodInfo, bool> ReplaysEvent(string eventName) =>
        m => m.GetCustomAttribute<ReplaysEventAttribute>()?.Event == eventName;

    private void InvokeReplayMethod(MethodBase method, PublishedEvent @event)
    {
        var type = method.GetParameters()[0].ParameterType;
        var args = new[] { type == typeof(PublishedEvent) ? @event : @event.DetailsAs(type) };
        method.Invoke(this, args);
    }

    /// <summary>
    /// Attribute for annotating a method that replays a specific event.<br />
    /// Add a method for each event that updates the state in a way that needs to be tracked.
    /// Add this attribute to the method to identify which event it handles.
    /// Add a parameter to contain the event details.<br />
    /// Methods with this attribute will be called once for each published event in order.<br />
    /// Example:
    /// <code>
    ///     [ReplaysEvent("SomePropertyIncreased")]
    ///     public void ReplaySomePropertyIncreased(Increment details)
    ///     {
    ///         this.SomeProperty += details.Amount;
    ///     }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    protected class ReplaysEventAttribute(string @event) : Attribute
    {
        public string Event { get; } = @event;
    }
}
