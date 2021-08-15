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
        private readonly List<Action<Event>> projections = new();
        private long lastReadPosition;

        public event EventHandler<EventsProcessedArgs>? EventsProcessed;

        public EventSource(IDbConnection connection, IPollingStrategy? pollingStrategy = null)
        {
            this.connection = connection;
            this.pollingStrategy = pollingStrategy ?? new DefaultPollingStrategy();
        }

        public void AddProjection(Action<Event> projection)
        {
            projections.Add(projection);
        }

        public void Start(long? lastRead = null)
        {
            lastReadPosition = lastRead ?? -1;
            NotifyNewEvents();
        }

        private void NotifyNewEvents()
        {
            connection.Open();
            var count = 0;
            try
            {
                using var reader = connection
                    .CreateCommand(
                        "SELECT * FROM Events WHERE position > @position ORDER BY version",
                        ("@position", lastReadPosition))
                    .ExecuteReader();

                while (reader.Read())
                {
                    count++;
                    var @event = ReadEvent(reader);
                    lastReadPosition = GetPosition(reader);
                    foreach (var projection in projections)
                        projection(@event);
                    EventsProcessed?.Invoke(this, new EventsProcessedArgs { Position = lastReadPosition });
                }
            }
            finally
            {
                connection.Close();

                var nextDelay = pollingStrategy.NextDelay(count);
                Task.Delay(nextDelay).ContinueWith(_ => { NotifyNewEvents(); });
            }
        }

        private static Event ReadEvent(IDataReader reader) => new(
            reader.GetString(reader.GetOrdinal("entity")),
            reader.GetString(reader.GetOrdinal("name")),
            reader.GetString(reader.GetOrdinal("details")));

        private static long GetPosition(IDataRecord record) => record.GetInt64(record.GetOrdinal("position"));

        private class DefaultPollingStrategy : IPollingStrategy
        {
            public int NextDelay(int count) => count == 0 ? 60_000 : 1_000;
        }
    }
}
