using Segerfeldt.EventStore.Refactoring;

namespace RefactoringWebApplication;

internal class TransformationStrategy : ITransformationStrategy
{
    public IEnumerable<TranslatedEvent> TransformPublishedBatch(IEnumerable<SourceEvent> sourceEvents) => sourceEvents.Select(e => e.Unchanged);
}
