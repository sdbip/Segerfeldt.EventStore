using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public class EventSource
    {
        private readonly IDbConnection connection;
        private readonly IPositionTracker? tracker;
        private readonly IPollingStrategy pollingStrategy;
        private readonly Dictionary<string, ICollection<IReceptacle>> receptacles = new();
        private long lastReadPosition;

        /// <summary>Initializes a new <see cref="EventSource"/></summary>
        /// <param name="connection">the database that stores your entities and events</param>
        /// <param name="tracker"></param>
        /// <param name="pollingStrategy">a strategy for how often to poll for new events</param>
        public EventSource(IDbConnection connection, IPositionTracker? tracker = null, IPollingStrategy? pollingStrategy = null)
        {
            this.connection = connection;
            this.tracker = tracker;
            this.pollingStrategy = pollingStrategy ?? new DefaultPollingStrategy();
        }

        /// <summary>
        /// Register a projector that will be notified whenever new events occur
        /// </summary>
        /// <param name="receptacle">the projector to register</param>
        public void Register(IReceptacle receptacle)
        {
            foreach (var eventName in receptacle.AcceptedEventNames)
            {
                if (receptacles.ContainsKey(eventName.Name))
                    receptacles[eventName.Name].Add(receptacle);
                else
                    receptacles[eventName.Name] = new List<IReceptacle> {receptacle};
            }
        }

        /// <summary>Start projecting the source state</summary>
        public void StartReceiving()
        {
            lastReadPosition = tracker?.GetLastFinishedProjectionId() ?? -1;
            NotifyNewEvents();
        }

        private void NotifyNewEvents()
        {
            var eventGroups = GroupByPosition(ReadEvents(lastReadPosition));
            var count = 0;
            foreach (var (position, events) in eventGroups)
            {
                lastReadPosition = position;
                count += events.Count;

                tracker?.OnProjectionStarting(position);
                foreach (var @event in events) Notify(@event);
                tracker?.OnProjectionFinished(position);
            }

            var nextDelay = pollingStrategy.NextDelay(count);
            Task.Delay(nextDelay).ContinueWith(_ => NotifyNewEvents());
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

        private void Notify(Event @event)
        {
            try
            {
                var tasks = GetAcceptingReceptacles(@event)
                    .Select(async d => await d.ReceiveAsync(@event));
                Task.WhenAll(tasks).Wait();
            }
            catch
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        private IEnumerable<IReceptacle> GetAcceptingReceptacles(Event @event) =>
            receptacles.ContainsKey(@event.Name)
                ? receptacles[@event.Name].Where(d => d.Accepts(@event))
                : ImmutableList<IReceptacle>.Empty;

        private IEnumerable<Event> ReadEvents(long afterPosition)
        {
            connection.Open();
            try
            {
                using var reader = connection
                    .CreateCommand(
                        "SELECT Events.*, Entities.type FROM Events JOIN Entities ON Events.entity = Entities.id " +
                        "  WHERE position > @position ORDER BY position, version",
                        ("@position", afterPosition))
                    .ExecuteReader();

                while (reader.Read())
                    yield return ReadEvent(reader);
            }
            finally
            {
                connection.Close();
            }
        }

        private static Event ReadEvent(IDataRecord record) => new(
            record.GetString(record.GetOrdinal("entity")),
            record.GetString(record.GetOrdinal("name")),
            record.GetString(record.GetOrdinal("type")),
            record.GetString(record.GetOrdinal("details")),
            record.GetInt64(record.GetOrdinal("position")));

        private class DefaultPollingStrategy : IPollingStrategy
        {
            public int NextDelay(int count) => count == 0 ? 60_000 : 1_000;
        }
    }
}
