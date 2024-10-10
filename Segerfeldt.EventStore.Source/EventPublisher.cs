using Segerfeldt.EventStore.Source.Internals;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
public sealed class EventPublisher
{
    private readonly IConnectionPool connectionPool;

    internal EventPublisher(IConnectionPool connectionPool)
    {
        this.connectionPool = connectionPool;
    }

    public EventPublisher(DbConnection connection) : this(new OnDemandConnectionFactory(() => connection)) { }

    /// <summary>Publish all new changes since reconstituting an entity</summary>
    /// <param name="entities">the entities whose events to publish</param>
    /// <param name="actor">the actor/user who caused these changes</param>
    public async Task<UpdatedStorePosition> PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor)
    {
        var operation = new InsertSingleEventOperation(@event, entityId, type, actor);
        return await operation.ExecuteAsync(connectionPool.CreateConnection());
    }

    /// <summary>Publish a single event for an entity</summary>
    /// <param name="entityId">the unique identifier for this entity</param>
    /// <param name="type">the type of the entity if it has to be created</param>
    /// <param name="event">the event to publish</param>
    /// <param name="actor">the actor/user who caused this change</param>
    public async Task<UpdatedStorePosition> PublishChangesAsync(IEnumerable<IEntity> entities, string actor)
    {
        var operation = new InsertMultipleEventsOperation(entities, actor);
        return await operation.ExecuteAsync(connectionPool.CreateConnection());
    }
}
