using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
/// <param name="repository"></param>
/// <param name="eventPublisher"></param>
/// <param name="strategy">/param>
/// <param name="tracker"></param>
/// <param name="pollingStrategy">a strategy for how often to poll for new events</param>
public sealed class EventSource(EventSourceRepository repository, EventPublisher eventPublisher, ITransformationStrategy strategy, IProjectionTracker? tracker = null, IPollingStrategy? pollingStrategy = null)
{
    private readonly EventSourceRepository repository = repository;
    private readonly EventPublisher eventPublisher = eventPublisher;
    private readonly ITransformationStrategy strategy = strategy;
    private readonly IProjectionTracker? tracker = tracker;
    private readonly IPollingStrategy pollingStrategy = pollingStrategy ?? new DefaultPollingStrategy();

    private long lastReadPosition = -1;
    private CancellationTokenSource? currentDelay;

    /// <summary>Start projecting the source state</summary>
    public void BeginProjecting()
    {
        GetPositionFromTracker();
        PollEventsTable();
    }

    public void GetPositionFromTracker()
    {
        lastReadPosition = tracker?.GetLastFinishedPosition() ?? -1;
    }

    private void PollEventsTable()
    {
        currentDelay?.Cancel();
        while (true)
        {
            var numNotified = EmitEventsAtNextPosition();
            var nextDelay = pollingStrategy.NextDelay(numNotified);
            if (nextDelay > 0)
            {
                currentDelay = new CancellationTokenSource();
                Task.Delay(nextDelay, currentDelay.Token).ContinueWith(_ => PollEventsTable());
                break;
            }
        }
    }

    public int EmitEventsAtNextPosition() => Emit(repository.GetEventsAtNextPosition(lastReadPosition));

    public int Emit(IEnumerable<Event> unsortedEvents)
    {
        var events = unsortedEvents.Select(e => e.SourceEvent).ToList();
        events.Sort((e1, e2) => e1.Ordinal - e2.Ordinal);
        if (events.Count == 0) return 0;

        var metadata = unsortedEvents.First().Metadata;
        var translatedEvents = strategy.TransformPublishedBatch(events);
        eventPublisher.Publish(translatedEvents, metadata);

        lastReadPosition = metadata.Position;
        tracker?.OnProjectionFinished(lastReadPosition);

        return events.Count;
    }

    private sealed class DefaultPollingStrategy : IPollingStrategy
    {
        public int NextDelay(int count) => count == 0 ? 60_000 : 0;
    }
}
