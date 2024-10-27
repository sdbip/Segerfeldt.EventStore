using System.Collections.Generic;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>A transformation strategy</summary>
public interface ITransformationStrategy
{
    /// <summary>Updates the receptacle with an event</summary>
    IEnumerable<TranslatedEvent> TransformPublishedBatch(IEnumerable<SourceEvent> sourceEvents);
}

public record TranslatedEvent(Entity Entity, string Name, string Details);
public record Entity(string Id, string Type);
