using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
/// <param name="repository"></param>
/// <param name="database"></param>
/// <param name="receptacles"/></param>
/// <param name="tracker"></param>
/// <param name="pollingStrategy">a strategy for how often to poll for new events</param>
public sealed class EventSource(IEventSourceRepository repository, TargetDatabase database, ReceptacleCollection receptacles, IProjectionTracker? tracker = null, IPollingStrategy? pollingStrategy = null)
{
    private readonly IEventSourceRepository repository = repository;
    private readonly TargetDatabase database = database;
    private readonly IProjectionTracker tracker = tracker ?? new NullProjectionTracker();
    private readonly IPollingStrategy pollingStrategy = pollingStrategy ?? new DefaultPollingStrategy();
    private CancellationTokenSource? currentDelay;

    /// <summary>Start projecting the source state</summary>
    public void BeginProjecting()
    {
        PollEventsTable();
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
        var unsortedEvents = repository.GetEventsAsync(tracker.GetLastFinishedPosition() ?? -1, maxCount: 100).Result;
        return Emit(unsortedEvents, maxCount: 100);
    }

    public int Emit(IEnumerable<Event> unsortedEvents, int maxCount)
    {
        var eventGroups = GroupByPosition(unsortedEvents).ToList();
        // If the event batch is maxed out, the last position might be incomplete.
        if (eventGroups.SelectMany(e => e.events).Count() >= maxCount)
            eventGroups.RemoveAt(eventGroups.Count - 1);

        return Emit(eventGroups);
    }

    private int Emit(List<(long position, IImmutableList<Event> events)> eventGroups)
    {
        var count = eventGroups.Sum(it => it.events.Count);
        foreach (var (position, events) in eventGroups)
            tracker.ProjectingPosition(position, () =>
            {
                foreach (var @event in events) Emit(@event);
            });

        return count;
    }

    private static IEnumerable<(long position, IImmutableList<Event> events)> GroupByPosition(IEnumerable<Event> events)
    {
        var groupings = events.GroupBy(e => e.Position).ToList();
        groupings.Sort((a, b) => a.Key.CompareTo(b.Key));

        foreach (var grouping in groupings)
        {
            var group = grouping.ToList();
            group.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));

            yield return (position: grouping.Key, events: group.ToImmutableList());
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

    private sealed class NullProjectionTracker : IProjectionTracker
    {
        private long? lastProjectedPosition;
        public long? GetLastFinishedPosition() => lastProjectedPosition;

        public Task ProjectingPosition(long position, Action runProjection)
        {
            runProjection();
            lastProjectedPosition = position;
            return Task.CompletedTask;
        }
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
