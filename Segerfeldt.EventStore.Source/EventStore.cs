using System;
using System.Collections.Generic;
using System.Data;

namespace Segerfeldt.EventStore.Source
{
    public sealed class EventStore
    {
        private readonly IDbConnection connection;

        public EventStore(IDbConnection connection)
        {
            this.connection = connection;
        }

        public void Publish(EntityId entityId, UnpublishedEvent @event, string actor)
        {
            var command = new InsertEventsOperation(entityId, actor, @event);
            command.Execute(connection);
        }

        public void PublishChanges(IEntity entity, string actor)
        {
            var command = new InsertEventsOperation(entity.Id, actor, entity.UnpublishedEvents) { ExpectedVersion = entity.Version };
            command.Execute(connection);
        }

        public T Reconstitute<T>(EntityId id) where T : IEntity
        {
            var reader = connection.CreateCommand("SELECT * FROM Events WHERE entity = @entityId ORDER BY version", ("@entityId", id.ToString()))
                .ExecuteReader();
            var events = new List<PublishedEvent>();
            while (reader.Read())
            {
                events.Add(new PublishedEvent((string)reader["name"], (string)reader["details"]));
            }

            var constructor = typeof(T).GetConstructor(new[] { typeof(EntityId), typeof(EntityVersion) });
            if (constructor is null) throw new Exception("Invalid entity type. Constructor missing.");
            var entity = (T)constructor.Invoke(new object[] { id, EntityVersion.New });
            entity.ReplayEvents(events);
            return entity;
        }
    }
}
