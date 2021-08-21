using Segerfeldt.EventStore.Source.Internals;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public sealed class EventStore
    {
        private readonly DbConnection connection;

        /// <summary>Initialize a new <see cref="EventStore"/></summary>
        /// <param name="connection">a connection to the database that stores the state of entities as sequences of events</param>
        public EventStore(DbConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>Publish a single event for an entity</summary>
        /// <param name="entityId">the unique identifier for this entity</param>
        /// <param name="event">the event to publish</param>
        /// <param name="actor">the actor/user who caused this change</param>
        public void Publish(EntityId entityId, UnpublishedEvent @event, string actor)
        {
            PublishAsync(entityId, @event, actor).Wait();
        }

        /// <summary>Publish a single event for an entity</summary>
        /// <param name="entityId">the unique identifier for this entity</param>
        /// <param name="event">the event to publish</param>
        /// <param name="actor">the actor/user who caused this change</param>
        public async Task PublishAsync(EntityId entityId, UnpublishedEvent @event, string actor)
        {
            var operation = new InsertSingleEventOperation(@event, entityId, actor);
            await operation.ExecuteAsync(connection);
        }

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entity">the entity whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public void PublishChanges(IEntity entity, string actor)
        {
            PublishChanges(new[] { entity }, actor);
        }

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entity">the entity whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public async Task PublishChangesAsync(IEntity entity, string actor)
        {
            await PublishChangesAsync(new[] { entity }, actor);
        }

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entities">the entity whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public void PublishChanges(IEnumerable<IEntity> entities, string actor)
        {
            PublishChangesAsync(entities, actor).Wait();
        }

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entities">the entities whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public async Task PublishChangesAsync(IEnumerable<IEntity> entities, string actor)
        {
            var operation = new InsertEventsOperation(entities, actor);
            await operation.ExecuteAsync(connection);
        }

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="id">the unique identifier of the entity to reconstitute</param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        /// <returns>the entity with the specified <paramref name="id"/></returns>
        public TEntity? Reconstitute<TEntity>(EntityId id) where TEntity : class, IEntity =>
            ReconstituteAsync<TEntity>(id).Result;

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="id">the unique identifier of the entity to reconstitute</param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        /// <returns>the entity with the specified <paramref name="id"/></returns>
        public async Task<TEntity?> ReconstituteAsync<TEntity>(EntityId id) where TEntity : class, IEntity
        {
            var operation = new GetHistoryOperation(id);
            var history = await operation.ExecuteAsync(connection);
            if (history is null) return null;

            var entity = Instantiate<TEntity>(id, history.Version);
            entity.ReplayEvents(history.Events);
            return entity;
        }

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <returns>the complete history of the entity</returns>
        public EntityHistory? GetHistory(EntityId entityId) =>
            GetHistoryAsync(entityId).Result;

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <returns>the complete history of the entity</returns>
        public async Task<EntityHistory?> GetHistoryAsync(EntityId entityId)
        {
            var operation = new GetHistoryOperation(entityId);
            return await operation.ExecuteAsync(connection);
        }

        private static TEntity Instantiate<TEntity>(EntityId id, EntityVersion version) where TEntity : IEntity
        {
            var constructor = typeof(TEntity).GetConstructor(new[] { typeof(EntityId), typeof(EntityVersion) });
            if (constructor is null) throw new Exception("Invalid entity type. Constructor missing.");
            return (TEntity)constructor.Invoke(new object[] { id, version });
        }
    }
}
