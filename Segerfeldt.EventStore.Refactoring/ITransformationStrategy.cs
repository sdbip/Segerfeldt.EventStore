using System.Collections.Generic;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>A transformation strategy</summary>
public interface ITransformationStrategy
{
    /// <summary>Transforms all events in a given </summary>
    IEnumerable<TransformedEvent> TransformPublishedBatch(IEnumerable<SourceEvent> sourceEvents);
}

/// <summary>A source event transformed to the target event model</summary>
/// <param name="Entity">The entity associated with the event</param>
/// <param name="Name">The name of the event</param>
/// <param name="Details">The details structure to convert to JSON</param>
public record TransformedEvent(Entity Entity, string Name, object Details);
public record Entity(string Id, string Type);
