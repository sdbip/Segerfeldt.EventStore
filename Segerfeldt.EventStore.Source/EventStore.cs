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

        public TEntity Reconstitute<TEntity>(EntityId id) where TEntity : IEntity
        {
            var events = GetPublishedEvents(id);
            var entity = Instantiate<TEntity>(id, EntityVersion.New);
            entity.ReplayEvents(events);
            return entity;
        }

        private static TEntity Instantiate<TEntity>(EntityId id, EntityVersion version) where TEntity : IEntity
        {
            var constructor = typeof(TEntity).GetConstructor(new[] { typeof(EntityId), typeof(EntityVersion) });
            if (constructor is null) throw new Exception("Invalid entity type. Constructor missing.");
            return (TEntity)constructor.Invoke(new object[] { id, version });
        }

        private IEnumerable<PublishedEvent> GetPublishedEvents(EntityId id)
        {
            var reader = connection.CreateCommand("SELECT * FROM Events WHERE entity = @entityId ORDER BY version",
                    ("@entityId", id.ToString()))
                .ExecuteReader();

            while (reader.Read())
                yield return new PublishedEvent((string)reader["name"], (string)reader["details"]);
        }
    }
}
