using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>An event notifying that the state of an entity has changed at the source</summary>
/// <param name="SourceEvent">The event data that will need to be transformed</param>
/// <param name="Metadata">Metadata that will be kept as-is</param>
    /// Note: It is assumed that all events published with the
    /// same position will have the same metadata in total.
public record Event(SourceEvent SourceEvent, EventMetadata Metadata)
{
    internal static int SortOrder(Event left, Event right) => left.SourceEvent.Ordinal - right.SourceEvent.Ordinal;
}

/// <summary>The transformed part of an event.</summary>
/// <param name="Entity">The entity whose state is modelled by the event</param>
/// <param name="Name">The name of the event</param>
/// <param name="Details">The details structure (JSON) of the event</param>
/// <param name="Ordinal">The ordinal of this event in the entity stream</param>
public record SourceEvent(Entity Entity, string Name, string Details, int Ordinal)
{
    public TransformedEvent Unchanged => new(Entity, Name, Details);

    public T? DetailsAs<T>() => JSON.Deserialize<T>(Details);
}

/// <summary>Fixed metadata that is attached to a batch of events and maintained in the transformation</summary>
/// <param name="Position">The position in the stream whh the event (and potentially others) were added.</param>
/// <param name="Actor">The actor whose action caused the event (and its friends) to be published</param>
/// <param name="Timestamp">The point in time (as OADate) when the event was published.</param>
public record EventMetadata(long Position, string Actor, double Timestamp);
