using Segerfeldt.EventStore.Source.Internals;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public sealed class EventPublisher
    {
        private readonly DbConnection connection;

        /// <summary>Initialize a new <see cref="EventPublisher"/></summary>
        /// <param name="connection">a connection to the database that stores the state of entities as sequences of events</param>
        public EventPublisher(DbConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>Publish a single event for an entity</summary>
        /// <param name="entityId">the unique identifier for this entity</param>
        /// <param name="type">the type of the entity if it needs to be created</param>
        /// <param name="event">the event to publish</param>
        /// <param name="actor">the actor/user who caused this change</param>
        public StreamPosition Publish(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor) =>
            PublishAsync(entityId, type, @event, actor).Result;

        /// <summary>Publish a single event for an entity</summary>
        /// <param name="entityId">the unique identifier for this entity</param>
        /// <param name="type">the type of the entity if it has to be created</param>
        /// <param name="event">the event to publish</param>
        /// <param name="actor">the actor/user who caused this change</param>
        public async Task<StreamPosition> PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor)
        {
            var operation = new InsertSingleEventOperation(@event, entityId, type, actor);
            return await operation.ExecuteAsync(connection);
        }

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entity">the entity whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public StreamPosition PublishChanges(IEntity entity, string actor) =>
            PublishChangesAsync(entity, actor).Result;

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entity">the entity whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public async Task<StreamPosition> PublishChangesAsync(IEntity entity, string actor)
        {
            var streamPositions = await PublishChangesAsync(new[] { entity }, actor);
            return new StreamPosition(streamPositions.StorePosition, streamPositions.GetVersion(entity.Id));
        }

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entities">the entity whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public StreamPositions PublishChanges(IEnumerable<IEntity> entities, string actor) =>
            PublishChangesAsync(entities, actor).Result;

        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entities">the entities whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        public async Task<StreamPositions> PublishChangesAsync(IEnumerable<IEntity> entities, string actor)
        {
            var operation = new InsertEventsOperation(entities, actor);
            return await operation.ExecuteAsync(connection);
        }
    }
}
