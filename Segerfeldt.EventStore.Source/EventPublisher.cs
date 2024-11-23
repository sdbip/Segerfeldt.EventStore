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
    public void Publish(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor, IDbTransaction transaction) =>
        PublishAsync(entityId, type, @event, actor, repository.CreateOperation(transaction)).Wait();

    /// <summary>Publish all new changes since reconstituting an entity</summary>
    /// <param name="entities">the entities whose events to publish</param>
    /// <param name="actor">the actor/user who caused these changes</param>
    public async Task PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor)
    {
        var operation = await repository.BeginAtomicOperationAsync();

        try
        {
            await PublishAsync(entityId, type, @event, actor, operation);
            await operation.CommitAsync();
        }
        catch
        {
            await operation.AbortAsync();
            throw;
        }
    }

    private async Task PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor, IAtomicOperation operation)
    {
        var currentVersion = await repository.GetCurrentEntityVersionAsync(entityId, operation);
        if (currentVersion == EntityVersion.New)
            await repository.InsertEntityAsync(entityId, type, Ordinal.Zero, operation);
        else
            await repository.UpdateVersionAsync(entityId, currentVersion.Next().Ordinal!, operation);
        var position = await repository.GetNextPositionAsync(operation);
        await InsertEventsForEntitiesAsync(position, [(entityId, [@event])], actor, operation);
    }

    /// <summary>Publish a single event for an entity</summary>
    /// <param name="entityId">the unique identifier for this entity</param>
    /// <param name="type">the type of the entity if it has to be created</param>
    /// <param name="event">the event to publish</param>
    /// <param name="actor">the actor/user who caused this change</param>
    public void PublishChanges(IEnumerable<IEntity> entities, string actor, IDbTransaction transaction) =>
        PublishChangesAsync(entities, actor, repository.CreateOperation(transaction)).Wait();

    /// <summary>Publish a single event for an entity</summary>
    /// <param name="entityId">the unique identifier for this entity</param>
    /// <param name="type">the type of the entity if it has to be created</param>
    /// <param name="event">the event to publish</param>
    /// <param name="actor">the actor/user who caused this change</param>
    public async Task PublishChangesAsync(IEnumerable<IEntity> entities, string actor)
    {
        var operation = await repository.BeginAtomicOperationAsync();

        try
        {
            await PublishChangesAsync(entities, actor, operation);
            await operation.CommitAsync();
        }
        catch
        {
            await operation.AbortAsync();
            throw;
        }
    }

    private async Task PublishChangesAsync(IEnumerable<IEntity> entities, string actor, IAtomicOperation operation)
    {
        var changedEntities = entities.Where(e => e.UnpublishedEvents.Any());
        if (!changedEntities.Any()) return;

        foreach (var entity in changedEntities)
        {
            var currentVersion = await repository.GetCurrentEntityVersionAsync(entity.Id, operation);
            if (entity.Version != currentVersion)
                throw new ConcurrentUpdateException(entity.Version, currentVersion);

            if (currentVersion == EntityVersion.New)
                await repository.InsertEntityAsync(entity.Id, entity.Type, Ordinal.Zero, operation);
            else
                await repository.UpdateVersionAsync(entity.Id, currentVersion.NextOrdinal(), operation);
        }

        var position = await repository.GetNextPositionAsync(operation);
        await InsertEventsForEntitiesAsync(position, changedEntities.Select(e => (e.Id, e.UnpublishedEvents)), actor, operation);
    }

    private async Task InsertEventsForEntitiesAsync(Position position, IEnumerable<(EntityId, IEnumerable<UnpublishedEvent>)> entities, string actor, IAtomicOperation operation)
    {
        await Task.WhenAll(
            entities.Select(async entity =>
            {
                var (id, events) = entity;
                var nextOrdinal = await repository.GetNextOrdinalAsync(id, operation);
                foreach (var (@event, ordinal) in events.Zip(IncrementingOrdinalsFrom(nextOrdinal)))
                    await repository.InsertEventAsync(id, @event, actor, ordinal, position, operation);
            })
        );

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

internal static class EventPublisherRepositoryExtensions
{
    public static async Task<EntityVersion> GetCurrentEntityVersionAsync(this IEventPublisherRepository repository, EntityId entityId, IAtomicOperation operation)
    {
        var ordinal = await repository.GetCurrentVersionAsync(entityId, operation);
        return ordinal is null ? EntityVersion.New : new EntityVersion(Ordinal.Safe(ordinal.Value));
    }

    public static async Task<Ordinal> GetNextOrdinalAsync(this IEventPublisherRepository repository, EntityId entityId, IAtomicOperation operation)
    {
        var ordinal = await repository.GetHighestOrdinalAsync(entityId, operation);
        return ordinal.HasValue ? Ordinal.Safe(ordinal.Value + 1) : Ordinal.Zero;
    }

    public static async Task<Position> GetNextPositionAsync(this IEventPublisherRepository repository, IAtomicOperation operation)
    {
        var position = await repository.GetLastPositionAsync(operation);
        return position.HasValue ? Position.Safe(position.Value + 1) : Position.Zero;
    }
}
