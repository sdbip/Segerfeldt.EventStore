using Segerfeldt.EventStore.Source.Internals;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            var history = GetHistory(id);
            if (history is null) return null;

            var entity = Instantiate<TEntity>(id, history.Version);
            entity.ReplayEvents(history.Events);
            return entity;
        }

        public EntityHistory? GetHistory(EntityId entityId)
        {
            var command = connection.CreateCommand(
                "SELECT version FROM Entities WHERE id = @entityId;" +
                "SELECT * FROM Events WHERE entity = @entityId ORDER BY version",
                ("@entityId", entityId.ToString()));

            using var reader = command.ExecuteReader();
            var version = ReadEntityVersion(reader);
            return version is not null ? new EntityHistory(version, ReadEvents(reader)) : null;
        }

        private static EntityVersion? ReadEntityVersion(IDataReader reader) =>
            reader.Read() ? EntityVersion.Of((int)reader[0]) : null;

        private static IEnumerable<PublishedEvent> ReadEvents(IDataReader reader)
        {
            if (!reader.NextResult()) return ImmutableList<PublishedEvent>.Empty;

            var events = new List<PublishedEvent>();
            while (reader.Read())
                events.Add(new PublishedEvent((string)reader["name"], (string)reader["details"], (string)reader["actor"]));
            return events;
        }

        private static TEntity Instantiate<TEntity>(EntityId id, EntityVersion version) where TEntity : IEntity
        {
            var constructor = typeof(TEntity).GetConstructor(new[] { typeof(EntityId), typeof(EntityVersion) });
            if (constructor is null) throw new Exception("Invalid entity type. Constructor missing.");
            return (TEntity)constructor.Invoke(new object[] { id, version });
        }
    }
}
