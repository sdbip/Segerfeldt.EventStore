using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public delegate Task ProjectionDelegate(Event @event);

    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public class EventSource
    {
        /// <summary>Arguments for the <see cref="EventSource.EventsProcessed"/> event</summary>
        public class EventsProcessedArgs : EventArgs
        {
            /// <summary>The position that was handled. Always increasing.</summary>
            public long Position { get; init; }
        }

        private readonly IDbConnection connection;
        private readonly IPollingStrategy pollingStrategy;
        private readonly Dictionary<string, List<ProjectionDelegate>> projectionDelegates = new();
        private long lastReadPosition;

        /// <summary>Notification after events have been processed</summary>
        public event EventHandler<EventsProcessedArgs>? EventsProcessed;

        /// <summary>Initializes a new <see cref="EventSource"/></summary>
        /// <param name="connection">the database that stores your entities and events</param>
        /// <param name="pollingStrategy">a strategy for how often to poll for new events</param>
        public EventSource(IDbConnection connection, IPollingStrategy? pollingStrategy = null)
        {
            this.connection = connection;
            this.pollingStrategy = pollingStrategy ?? new DefaultPollingStrategy();
        }

        /// <summary>Adds a projection to be notified of events</summary>
        /// <param name="eventName">the name of the event that invokes this projection</param>
        /// <param name="delegate">the function to call when the event is encountered</param>
        public void AddProjectionDelegate(string eventName, ProjectionDelegate @delegate)
        {
            projectionDelegates.TryAdd(eventName, new List<ProjectionDelegate>());
            projectionDelegates[eventName].Add(@delegate);
        }

        /// <summary>Start processing new events</summary>
        /// <param name="processedPosition">The last position already processed</param>
        public void Start(long? processedPosition = null)
        {
            lastReadPosition = processedPosition ?? -1;
            NotifyNewEvents();
        }

        private void NotifyNewEvents()
        {
            var count = 0;
            var events = ReadEvents();
            foreach (var @event in events)
            {
                count++;
                lastReadPosition = @event.Position;
                Notify(@event);

                EventsProcessed?.Invoke(this, new EventsProcessedArgs { Position = lastReadPosition });
            }

            var nextDelay = pollingStrategy.NextDelay(count);
            Task.Delay(nextDelay).ContinueWith(_ => { NotifyNewEvents(); });
        }

        private void Notify(Event @event)
        {
            if (projectionDelegates.TryGetValue(@event.Name, out var delegates))
                Task.WhenAll(delegates.Select(d => d.Invoke(@event))).Wait();
        }

        private IEnumerable<Event> ReadEvents()
        {
            try
            {
                using var reader = connection
                    .CreateCommand(
                        "SELECT * FROM Events WHERE position > @position ORDER BY version",
                        ("@position", lastReadPosition))
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
            record.GetString(record.GetOrdinal("details")),
            record.GetInt64(record.GetOrdinal("position")));

        private class DefaultPollingStrategy : IPollingStrategy
        {
            public int NextDelay(int count) => count == 0 ? 60_000 : 1_000;
        }
    }
}
