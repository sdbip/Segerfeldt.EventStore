using Segerfeldt.EventStore.Source.CommandAPI;
using Segerfeldt.EventStore.Source.Internals;

using System.Collections.Generic;
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
    public async Task<UpdatedStorePosition> PublishAsync(EntityId entityId, EntityType type, UnpublishedEvent @event, string actor)
    {
        var connection = repository.CreateConnection();
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();

        try
        {
            var currentVersion = await repository.GetCurrentEntityVersionAsync(entityId, transaction);
            if (currentVersion.IsNew) await repository.InsertEntityAsync(entityId, type, EntityVersion.Zero, transaction);

            var result = await InsertEventsForEntities([(entityId, currentVersion, [@event])], actor, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally { await connection.CloseAsync(); }
    }

    /// <summary>Publish a single event for an entity</summary>
    /// <param name="entityId">the unique identifier for this entity</param>
    /// <param name="type">the type of the entity if it has to be created</param>
    /// <param name="event">the event to publish</param>
    /// <param name="actor">the actor/user who caused this change</param>
    public async Task<UpdatedStorePosition> PublishChangesAsync(IEnumerable<IEntity> entities, string actor)
    {
        var connection = repository.CreateConnection();
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var entity in entities)
            {
                var currentVersion = await repository.GetCurrentEntityVersionAsync(entity.Id, transaction);
                if (entity.Version != currentVersion)
                    throw new ConcurrentUpdateException(entity.Version, currentVersion);

                if (currentVersion.IsNew) await repository.InsertEntityAsync(entity.Id, entity.Type, entity.Version, transaction);
            }

            var result = await InsertEventsForEntities(entities.Where(e => e.UnpublishedEvents.Any()).Select(e => (e.Id, e.Version, e.UnpublishedEvents)), actor, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally { await connection.CloseAsync(); }
    }

    private async Task<UpdatedStorePosition> InsertEventsForEntities(IEnumerable<(EntityId, EntityVersion, IEnumerable<UnpublishedEvent>)> entities, string actor, DbTransaction transaction)
    {
        var position = await repository.GetNextPositionAsync(transaction);

        var entityVersions = await Task.WhenAll(
            entities.Select(async entity =>
            {
                var (id, currentVersion, events) = entity;
                var nextOrdinal = await repository.GetNextOrdinalAsync(id, transaction);
                foreach (var (@event, ordinal) in events.Zip(IncrementingOrdinalsFrom(nextOrdinal)))
                    await repository.InsertEventAsync(id, @event, actor, ordinal, position, transaction);

                var (_, lastInsertedOrdinal) = events.Zip(IncrementingOrdinalsFrom(nextOrdinal)).Last();
                await repository.UpdateVersionAsync(id, currentVersion.Next(), transaction);
                return (id, currentVersion.Next());
            })
        );

        return new UpdatedStorePosition(position, entityVersions);

        // ReSharper disable once IteratorNeverReturns
        static IEnumerable<EventOrdinal> IncrementingOrdinalsFrom(EventOrdinal first)
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
