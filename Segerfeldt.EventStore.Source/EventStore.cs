using Segerfeldt.EventStore.Source.Internals;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public sealed class EventStore
    {
        private readonly IDbConnection connection;

        /// <summary>Initialize a new <see cref="EventStore"/></summary>
        /// <param name="connection">a connection to the database that stores the state of entities as sequences of events</param>
        public EventStore(IDbConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>Publish a single event for an entity</summary>
        /// <param name="entityId">the unique identifier for this entity</param>
        /// <param name="event">the event to publish</param>
        /// <param name="actor">the actor/user who caused this change</param>
        public void Publish(EntityId entityId, UnpublishedEvent @event, string actor)
        {
            var command = new InsertEventsOperation(entityId, actor, @event);
            command.Execute(connection);
        }

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entity">the entity whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public void PublishChanges(IEntity entity, string actor)
        {
            var command = new InsertEventsOperation(entity.Id, actor, entity.UnpublishedEvents) { ExpectedVersion = entity.Version };
            command.Execute(connection);
        }

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="id">the unique identifier of the entity to reconstitute</param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        /// <returns>the entity with the specified <paramref name="id"/></returns>
        public TEntity? Reconstitute<TEntity>(EntityId id) where TEntity : class, IEntity
        {
            var history = GetHistory(id);
            if (history is null) return null;

            var entity = Instantiate<TEntity>(id, history.Version);
            entity.ReplayEvents(history.Events);
            return entity;
        }

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <returns>the complete history of the entity</returns>
        public EntityHistory? GetHistory(EntityId entityId)
        {
            var command = connection.CreateCommand(
                "SELECT version FROM Entities WHERE id = @entityId;" +
                "SELECT * FROM Events WHERE entity = @entityId ORDER BY version",
                ("@entityId", entityId.ToString()));

            connection.Open();
            using var reader = command.ExecuteReader();
            var version = ReadEntityVersion(reader);
            if (version is null)
            {
                connection.Close();
                return null;
            }

            var events = ReadEvents(reader);

            connection.Close();
            return new EntityHistory(version, events);

        }

        private static EntityVersion? ReadEntityVersion(IDataReader reader) =>
            reader.Read() ? EntityVersion.Of((int)reader[0]) : null;

        private static IEnumerable<PublishedEvent> ReadEvents(IDataReader reader)
        {
            if (!reader.NextResult()) return ImmutableList<PublishedEvent>.Empty;

            var events = new List<PublishedEvent>();
            while (reader.Read())
            {
                var name = (string)reader["name"];
                var details = (string)reader["details"];
                var actor = (string)reader["actor"];
                var timestamp = reader["timestamp"] as DateTime? ?? DateTime.MinValue;
                timestamp = new DateTime(timestamp.Ticks, DateTimeKind.Utc);
                events.Add(new PublishedEvent(name, details, actor, timestamp));
            }

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
