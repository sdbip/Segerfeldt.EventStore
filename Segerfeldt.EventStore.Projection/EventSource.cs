using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public class EventSource
    {
        public class EventsProcessedArgs : EventArgs
        {
            public long Position { get; init; }
        }

        private readonly IDbConnection connection;
        private readonly IPollingStrategy pollingStrategy;
        private readonly Dictionary<string, List<Action<Event>>> projections = new();
        private long lastReadPosition;

        public event EventHandler<EventsProcessedArgs>? EventsProcessed;

        public EventSource(IDbConnection connection, IPollingStrategy? pollingStrategy = null)
        {
            this.connection = connection;
            this.pollingStrategy = pollingStrategy ?? new DefaultPollingStrategy();
        }

        public void AddProjection(string eventName, Action<Event> projection)
        {
            projections.Add(eventName, new List<Action<Event>> { projection });
        }

        public void Start(long? lastRead = null)
        {
            lastReadPosition = lastRead ?? -1;
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
            if (!projections.TryGetValue(@event.Name, out var eventProjections)) return;

            foreach (var projection in eventProjections)
            {
                try
                {
                    projection.Invoke(@event);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception.Message);
                    Console.Error.WriteLine(exception.StackTrace);
                }
            }
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
