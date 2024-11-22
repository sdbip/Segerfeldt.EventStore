using Segerfeldt.EventStore.Source.CommandAPI;
using Segerfeldt.EventStore.Source.Internals;

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
public sealed class EventPublisher(IEventPublisherRepository repository)
{
    private readonly IEventPublisherRepository repository = repository;

    public EventPublisher(DbConnection connection) : this(new EventPublisherRepository(EventStoreConnectionFactory.Singleton(connection))) { }

    /// <summary>Publish all new changes since reconstituting an entity</summary>
    /// <param name="entities">the entities whose events to publish</param>
    /// <param name="actor">the actor/user who caused these changes</param>
    public UpdatedStorePosition Publish(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor, IDbTransaction transaction) =>
        PublishAsync(entityId, type, @event, actor, repository.CreateOperation(transaction)).Result;

    /// <summary>Publish all new changes since reconstituting an entity</summary>
    /// <param name="entities">the entities whose events to publish</param>
    /// <param name="actor">the actor/user who caused these changes</param>
    public async Task<UpdatedStorePosition> PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor)
    {
        var operation = await repository.BeginAtomicOperationAsync();

        try
        {
            var result = await PublishAsync(entityId, type, @event, actor, operation);
            await operation.CommitAsync();
            return result;
        }
        catch
        {
            await operation.AbortAsync();
            throw;
        }
    }

    private async Task<UpdatedStorePosition> PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor, IAtomicOperation operation)
    {
        var currentVersion = await repository.GetCurrentEntityVersionAsync(entityId, operation);
        if (currentVersion.IsNew) await repository.InsertEntityAsync(entityId, type, EntityVersion.Zero, operation);
        return await InsertEventsForEntitiesAsync([(entityId, currentVersion, [@event])], actor, operation);
    }

    /// <summary>Publish a single event for an entity</summary>
    /// <param name="entityId">the unique identifier for this entity</param>
    /// <param name="type">the type of the entity if it has to be created</param>
    /// <param name="event">the event to publish</param>
    /// <param name="actor">the actor/user who caused this change</param>
    public UpdatedStorePosition PublishChanges(IEnumerable<IEntity> entities, string actor, IDbTransaction transaction) =>
        PublishChangesAsync(entities, actor, repository.CreateOperation(transaction)).Result;

    /// <summary>Publish a single event for an entity</summary>
    /// <param name="entityId">the unique identifier for this entity</param>
    /// <param name="type">the type of the entity if it has to be created</param>
    /// <param name="event">the event to publish</param>
    /// <param name="actor">the actor/user who caused this change</param>
    public async Task<UpdatedStorePosition> PublishChangesAsync(IEnumerable<IEntity> entities, string actor)
    {
        var operation = await repository.BeginAtomicOperationAsync();

        try
        {
            var result = await PublishChangesAsync(entities, actor, operation);
            await operation.CommitAsync();
            return result;
        }
        catch
        {
            await operation.AbortAsync();
            throw;
        }
    }

    private async Task<UpdatedStorePosition> PublishChangesAsync(IEnumerable<IEntity> entities, string actor, IAtomicOperation operation)
    {
        foreach (var entity in entities)
        {
            var currentVersion = await repository.GetCurrentEntityVersionAsync(entity.Id, operation);
            if (entity.Version != currentVersion)
                throw new ConcurrentUpdateException(entity.Version, currentVersion);

            if (currentVersion.IsNew) await repository.InsertEntityAsync(entity.Id, entity.Type, entity.Version, operation);
        }

        return await InsertEventsForEntitiesAsync(entities.Where(e => e.UnpublishedEvents.Any()).Select(e => (e.Id, e.Version, e.UnpublishedEvents)), actor, operation);
    }

    private async Task<UpdatedStorePosition> InsertEventsForEntitiesAsync(IEnumerable<(EntityId, EntityVersion, IEnumerable<UnpublishedEvent>)> entities, string actor, IAtomicOperation operation)
    {
        var position = await repository.GetNextPositionAsync(operation);

        var entityVersions = await Task.WhenAll(
            entities.Select(async entity =>
            {
                var (id, currentVersion, events) = entity;
                var nextOrdinal = await repository.GetNextOrdinalAsync(id, operation);
                foreach (var (@event, ordinal) in events.Zip(IncrementingOrdinalsFrom(nextOrdinal)))
                    await repository.InsertEventAsync(id, @event, actor, ordinal, position, operation);

                var (_, lastInsertedOrdinal) = events.Zip(IncrementingOrdinalsFrom(nextOrdinal)).Last();
                await repository.UpdateVersionAsync(id, currentVersion.Next(), operation);
                return (id, currentVersion.Next());
            })
        );

        return new UpdatedStorePosition(position, entityVersions);

        // ReSharper disable once IteratorNeverReturns
        static IEnumerable<Ordinal> IncrementingOrdinalsFrom(Ordinal first)
        {
            var next = first;
            while (true)
            {
                yield return next;
                next = next.Next();
            }
        }
    }
}
