using System;
using System.Collections.Generic;
using System.Data;

namespace Segerfeldt.EventStore.Projection
{
    public class EventSource
    {
        private readonly IDbConnection connection;
        private readonly List<Action<Event>> projections = new();

        public EventSource(IDbConnection connection)
        {
            this.connection = connection;
        }

        public void AddProjection(Action<Event> projection)
        {
            projections.Add(projection);
        }

        public void Start()
        {
            connection.Open();
            var reader = connection.CreateCommand("SELECT * FROM Events ORDER BY version").ExecuteReader();
            while (reader.Read())
            {
                var @event = new Event((string)reader["name"]);
                foreach (var projection in projections)
                    projection(@event);
            }
        }
    }
}
