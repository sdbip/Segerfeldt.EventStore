using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public class EventSource
    {
        private readonly IDbConnection connection;
        private readonly IDelayConfiguration delayConfiguration;
        private readonly List<Action<Event>> projections = new();
        private long lastReadPosition;

        public EventSource(IDbConnection connection, IDelayConfiguration delayConfiguration)
        {
            this.connection = connection;
            this.delayConfiguration = delayConfiguration;
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
                var reader = connection
                    .CreateCommand(
                        "SELECT * FROM Events WHERE position > @position ORDER BY version",
                        ("@position", lastReadPosition))
                    .ExecuteReader();

                while (reader.Read())
                {
                    count++;
                    var @event = new Event(
                        reader.GetString(reader.GetOrdinal("entity")),
                        reader.GetString(reader.GetOrdinal("name")),
                        reader.GetString(reader.GetOrdinal("details")));
                    lastReadPosition = reader.GetInt64(reader.GetOrdinal("position"));
                    foreach (var projection in projections)
                        projection(@event);
                }
            }
            finally
            {
                connection.Close();

                var nextDelay = delayConfiguration.NextDelay(count);
                Task.Delay(nextDelay).ContinueWith(_ => { NotifyNewEvents(); });
            }
        }
    }
}
