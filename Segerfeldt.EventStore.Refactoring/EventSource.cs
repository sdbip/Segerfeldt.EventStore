using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
/// <param name="repository"></param>
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
        var numNotified = PollEventsTableOnce();

        var nextDelay = pollingStrategy.NextDelay(numNotified);
        currentDelay = new CancellationTokenSource();
        Task.Delay(nextDelay, currentDelay.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled) PollEventsTable();
        });
    }

    public int PollEventsTableOnce()
    {
        var readEvents = repository.GetEvents(lastReadPosition);
        return Emit(readEvents);
    }

    public int Emit(IEnumerable<Event> unsortedEvents)
    {
        var eventGroups = GroupByPosition(unsortedEvents);
        var batch = new List<(long position, List<Event> events)>();
        var count = 0;
        foreach (var (position, events) in eventGroups)
        {
            count += events.Count;
            batch.Add((position, events.ToList()));
            if (count > 100) break;
        }

        foreach (var (position, events) in batch)
        {
            tracker?.OnProjectionStarting(position);
            try
            {
                var currentEvents = events.Select(e => new SourceEvent(new Entity(e.EntityId, e.EntityType), e.Name, e.Details));
                var translatedEvents = strategy.TransformPublishedBatch(currentEvents);
                eventPublisher.Publish(translatedEvents, position, events[0].Actor, events[0].Timestamp);
            }
            catch
            {
                tracker?.OnProjectionError(position);
                throw;
            }
            lastReadPosition = position;
            tracker?.OnProjectionFinished(position);
        }

        return count;
    }

    private static IEnumerable<(long position, IImmutableList<Event> events)> GroupByPosition(IEnumerable<Event> events)
    {
        var currentPosition = -1L;
        var nextBatch = new List<Event>();
        foreach (var @event in events)
        {
            if (@event.Position != currentPosition)
            {
                if (nextBatch.Count > 0)
                    yield return (currentPosition, nextBatch.ToImmutableList());
                nextBatch.Clear();
                currentPosition = @event.Position;
            }

            nextBatch.Add(@event);
        }

        if (nextBatch.Count > 0)
        {
            nextBatch.Sort((e1, e2) => e1.Ordinal - e2.Ordinal);
            yield return (currentPosition, nextBatch.ToImmutableList());
        }
    }

    private sealed class DefaultPollingStrategy : IPollingStrategy
    {
        public int NextDelay(int count) => count == 0 ? 60_000 : 1_000;
    }
}
