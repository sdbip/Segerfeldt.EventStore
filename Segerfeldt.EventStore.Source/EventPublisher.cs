using Segerfeldt.EventStore.Source.Internals;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public interface IEventPublisher
    {
        /// <summary>Publish all new changes since reconstituting an entity</summary>
        /// <param name="entities">the entities whose events to publish</param>
        /// <param name="actor">the actor/user who caused these changes</param>
        Task<UpdatedStorePosition> PublishChangesAsync(IEnumerable<IEntity> entities, string actor);

        /// <summary>Publish a single event for an entity</summary>
        /// <param name="entityId">the unique identifier for this entity</param>
        /// <param name="type">the type of the entity if it has to be created</param>
        /// <param name="event">the event to publish</param>
        /// <param name="actor">the actor/user who caused this change</param>
        Task<UpdatedStorePosition> PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor);
    }

    /// <inheritdoc />
    public sealed class EventPublisher : IEventPublisher
    {
        private readonly IConnectionPool connectionPool;

        /// <summary>Initialize a new <see cref="EventPublisher"/></summary>
        /// <param name="connectionPool">opens connections to the database that stores the state of entities as sequences of events</param>
        public EventPublisher(IConnectionPool connectionPool) => this.connectionPool = connectionPool;

        /// <inhericdoc />
        public async Task<UpdatedStorePosition> PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor)
        {
            var operation = new InsertSingleEventOperation(@event, entityId, type, actor);
            return await operation.ExecuteAsync(connectionPool.CreateConnection());
        }

        /// <inhericdoc />
        public async Task<UpdatedStorePosition> PublishChangesAsync(IEnumerable<IEntity> entities, string actor)
        {
            var operation = new InsertMultipleEventsOperation(entities, actor);
            return await operation.ExecuteAsync(connectionPool.CreateConnection());
        }
    }
}
