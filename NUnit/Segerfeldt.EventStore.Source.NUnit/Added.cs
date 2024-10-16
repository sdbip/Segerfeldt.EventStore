using JetBrains.Annotations;

namespace Segerfeldt.EventStore.Source.NUnit;

/// <summary>Helper methods for verivying whether an <see cref="IEntity"/> has added unpublished events.</summary>
[PublicAPI]
public static class Added
{
    /// <summary>The entity has added no new events whatsoever.</summary>
    public static readonly AddedNoEventsConstraint NoEvents = new();
    /// <summary>The entity has added at least one event with a specific name</summary>
    /// <param name="name">The expected <see cref="UnpublishedEvent.Name"/></param>
    public static AddedEventConstraint Event(string name)  => new(name);
}
