using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
public sealed class EventSource
{
    private readonly IEventSourceRepository repository;
    private readonly IPositionTracker? tracker;
    private readonly IPollingStrategy pollingStrategy;
    private readonly Dictionary<string, ICollection<IReceptacle>> receptacles = new();
    private long lastReadPosition;
    private CancellationTokenSource? currentDelay;

    /// <summary>Initializes a new <see cref="EventSource"/></summary>
    /// <param name="connectionPool">opens connections to the database that stores your entities and events</param>
    /// <param name="tracker"></param>
    /// <param name="pollingStrategy">a strategy for how often to poll for new events</param>
    public EventSource(IEventSourceRepository repository, IPositionTracker? tracker = null, IPollingStrategy? pollingStrategy = null)
    {
        this.repository = repository;
        this.tracker = tracker;
        this.pollingStrategy = pollingStrategy ?? new DefaultPollingStrategy();
    }

    /// <summary>
    /// Register a projector that will be notified whenever new events occur
    /// </summary>
    /// <param name="receptacle">the projector to register</param>
    public void Register(IReceptacle receptacle)
    {
        foreach (var eventName in receptacle.AcceptedEvents)
        {
            if (receptacles.ContainsKey(eventName))
                receptacles[eventName].Add(receptacle);
            else
                receptacles[eventName] = new List<IReceptacle> {receptacle};
        }
    }

    /// <summary>Start projecting the source state</summary>
    public void BeginProjecting()
    {
        lastReadPosition = tracker?.GetLastFinishedProjectionId() ?? -1;
        PollEventsTable();
    }

    private void PollEventsTable()
    {
        currentDelay?.Cancel();
        var readEvents = repository.GetEvents(lastReadPosition);
        var numNotified = Emit(readEvents);

        var nextDelay = pollingStrategy.NextDelay(numNotified);
        currentDelay = new CancellationTokenSource();
        Task.Delay(nextDelay, currentDelay.Token).ContinueWith(t => {
            if (!t.IsCanceled) PollEventsTable();
        });
    }

    public int Emit(IEnumerable<Event> readEvents)
    {
        var eventGroups = GroupByPosition(readEvents);
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
            lastReadPosition = position;
            tracker?.OnProjectionStarting(position);
            foreach (var @event in events) Emit(@event);
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

        if (nextBatch.Count > 0) yield return (currentPosition, nextBatch.ToImmutableList());
    }

    private void Emit(Event @event)
    {
        try
        {
            var tasks = GetReceptacles(@event)
                .Select(async d => await d.ReceiveAsync(@event));
            Task.WhenAll(tasks).Wait();
        }
        catch
        {
            System.Diagnostics.Debugger.Break();
        }
    }

    private IEnumerable<IReceptacle> GetReceptacles(Event @event) =>
        receptacles.ContainsKey(@event.Name)
            ? receptacles[@event.Name]
            : ImmutableList<IReceptacle>.Empty;

    private sealed class DefaultPollingStrategy : IPollingStrategy
    {
        public int NextDelay(int count) => count == 0 ? 60_000 : 1_000;
    }
}
