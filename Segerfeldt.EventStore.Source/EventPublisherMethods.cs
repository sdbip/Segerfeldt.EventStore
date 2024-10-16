using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

public static class EventPublisherMethods
{
    /// <summary>Publish a single event for an entity</summary>
    /// <param name="eventPublisher"></param>
    /// <param name="entityId">the unique identifier for this entity</param>
    /// <param name="type">the type of the entity if it needs to be created</param>
    /// <param name="event">the event to publish</param>
    /// <param name="actor">the actor/user who caused this change</param>
    public static UpdatedStorePosition Publish(this EventPublisher eventPublisher, EntityId entityId, EntityType type, UnpublishedEvent @event, string actor) =>
        eventPublisher.PublishAsync(entityId, type, @event, actor).Result;

    /// <summary>Publish all new changes since reconstituting an entity</summary>
    /// <param name="eventPublisher"></param>
    /// <param name="entity">the entity whose events to publish</param>
    /// <param name="actor">the actor/user who caused these changes</param>
    public static UpdatedStorePosition PublishChanges(this EventPublisher eventPublisher, IEntity entity, string actor) =>
        eventPublisher.PublishChangesAsync(entity, actor).Result;

    /// <summary>Publish all new changes since reconstituting an entity</summary>
    /// <param name="eventPublisher"></param>
    /// <param name="entity">the entity whose events to publish</param>
    /// <param name="actor">the actor/user who caused these changes</param>
    public static async Task<UpdatedStorePosition> PublishChangesAsync(this EventPublisher eventPublisher, IEntity entity, string actor) =>
        await eventPublisher.PublishChangesAsync(new[] { entity }, actor);

    /// <summary>Publish all new changes since reconstituting an entity</summary>
    /// <param name="eventPublisher"></param>
    /// <param name="entities">the entity whose events to publish</param>
    /// <param name="actor">the actor/user who caused these changes</param>
    public static UpdatedStorePosition PublishChanges(this EventPublisher eventPublisher, IEnumerable<IEntity> entities, string actor) =>
        eventPublisher.PublishChangesAsync(entities, actor).Result;
}
