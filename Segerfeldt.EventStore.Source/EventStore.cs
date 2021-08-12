using Segerfeldt.EventStore.Source.Internals;

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

        public TEntity? Reconstitute<TEntity>(EntityId id) where TEntity : class, IEntity
        {
            var version = GetEntityVersion(id);
            if (version is null) return null;

            var events = GetPublishedEvents(id);
            var entity = Instantiate<TEntity>(id, version);
            entity.ReplayEvents(events);
            return entity;
        }

        private static TEntity Instantiate<TEntity>(EntityId id, EntityVersion version) where TEntity : IEntity
        {
            var constructor = typeof(TEntity).GetConstructor(new[] { typeof(EntityId), typeof(EntityVersion) });
            if (constructor is null) throw new Exception("Invalid entity type. Constructor missing.");
            return (TEntity)constructor.Invoke(new object[] { id, version });
        }

        private EntityVersion? GetEntityVersion(EntityId id)
        {
            var command = connection.CreateCommand(
                "SELECT version FROM Entities WHERE id = @entityId",
                ("@entityId", id.ToString()));
            var version = command.ExecuteScalar();
            return version is null ? null : EntityVersion.Of((int)version);
        }

        private IEnumerable<PublishedEvent> GetPublishedEvents(EntityId id)
        {
            var command = connection.CreateCommand(
                "SELECT * FROM Events WHERE entity = @entityId ORDER BY version",
                ("@entityId", id.ToString()));
            var reader = command.ExecuteReader();
            var result = new List<PublishedEvent>();
            while (reader.Read())
                result.Add(new PublishedEvent((string)reader["name"], (string)reader["details"]));
            reader.Dispose();
            return result;
        }
    }
}
