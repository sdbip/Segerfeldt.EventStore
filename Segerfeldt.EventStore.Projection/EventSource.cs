using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
/// <param name="repository"></param>
/// <param name="tracker"></param>
/// <param name="pollingStrategy">a strategy for how often to poll for new events</param>
public sealed class EventSource(IEventSourceRepository repository, ReceptacleCollection receptacles, IProjectionTracker? tracker = null, IPollingStrategy? pollingStrategy = null)
{
    private readonly IEventSourceRepository repository = repository;
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

    internal void GetPositionFromTracker()
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

    internal int PollEventsTableOnce()
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
            try { foreach (var @event in events) Emit(@event); }
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

    private void Emit(Event @event)
    {
        foreach (var receptacle in GetReceptacles(@event)) receptacle.Update(@event);
    }

    private IEnumerable<IReceptacle> GetReceptacles(Event @event) => receptacles.GetReceptacles(@event.Name);

    private sealed class DefaultPollingStrategy : IPollingStrategy
    {
        public int NextDelay(int count) => count == 0 ? 60_000 : 1_000;
    }
}

public class ReceptacleCollection
{
    private readonly Dictionary<string, ICollection<IReceptacle>> receptacles = [];

    public ReceptacleCollection()
    {
    }

    public ReceptacleCollection(IEnumerable<IReceptacle> enumerable)
    {
        foreach(var receptacle in enumerable) Add(receptacle);
    }

    public void Add(IReceptacle receptacle)
    {
        foreach (var eventName in receptacle.AcceptedEvents)
        {
            if (receptacles.TryGetValue(eventName, out var value))
                value.Add(receptacle);
            else
                receptacles[eventName] = [receptacle];
        }
    }

    public IEnumerable<IReceptacle> GetReceptacles(string eventName) =>
        receptacles.TryGetValue(eventName, out var value) ? value : ImmutableList<IReceptacle>.Empty;
}
