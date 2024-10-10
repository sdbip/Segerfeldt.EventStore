using Segerfeldt.EventStore.Source.Snapshots;

using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

public static class EntityStoreMethods
{
    /// <summary>Reconstitute the state of an entity from published events</summary>
    /// <param name="entityStore"></param>
    /// <param name="id">the unique identifier of the entity to reconstitute</param>
    /// <param name="type"></param>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    /// <returns>the entity with the specified <paramref name="id"/></returns>
    public static TEntity? Reconstitute<TEntity>(this EntityStore entityStore, EntityId id, EntityType type) where TEntity : class, IEntity =>
        entityStore.ReconstituteAsync<TEntity>(id, type).Result;

    /// <summary>Reconstitute the state of an entity from published events</summary>
    /// <param name="entityStore"></param>
    /// <param name="id">the unique identifier of the entity to reconstitute</param>
    /// <param name="type"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    /// <returns>the entity with the specified <paramref name="id"/></returns>
    public static async Task<TEntity?> ReconstituteAsync<TEntity>(this EntityStore entityStore, EntityId id, EntityType type, CancellationToken cancellationToken = default) where TEntity : class, IEntity =>
        await entityStore.ReconstituteAsync(new NeverSnapshot<TEntity>(id, type), cancellationToken);

    /// <summary>Reconstitute the state of an entity from published events</summary>
    /// <param name="entityStore"></param>
    /// <param name="snapshot">the snapshot of the entity</param>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    public static TEntity? Reconstitute<TEntity>(this EntityStore entityStore, ISnapshot<TEntity> snapshot) where TEntity : class, IEntity =>
        entityStore.ReconstituteAsync(snapshot).Result;

    /// <summary>Reconstitute the state of an entity from published events</summary>
    /// <param name="entityStore"></param>
    /// <param name="snapshot">the snapshot of the entity</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    public static async Task<TEntity?> ReconstituteAsync<TEntity>(this EntityStore entityStore, ISnapshot<TEntity> snapshot, CancellationToken cancellationToken = default) where TEntity : class, IEntity
    {
        var history = await entityStore.GetHistoryAsync(snapshot.Id, snapshot.Version, cancellationToken);
        if (history is null) return snapshot.Version.IsNew ? null : throw new UnknownEntityException(snapshot.Id);
        if (history.Type != snapshot.EntityType) throw new IncorrectTypeException(snapshot.EntityType, history.Type);
        return entityStore.RestoreEntity(snapshot, history);
    }

    /// <summary>Get the historical data about an entity</summary>
    /// <param name="entityStore"></param>
    /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
    /// <returns>the complete history of the entity</returns>
    public static EntityHistory? GetHistory(this EntityStore entityStore, EntityId entityId) =>
        entityStore.GetHistoryAsync(entityId).Result;

    /// <summary>Get the historical data about an entity</summary>
    /// <param name="entityStore"></param>
    /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
    /// <param name="cancellationToken"></param>
    /// <returns>the complete history of the entity</returns>
    public static async Task<EntityHistory?> GetHistoryAsync(this EntityStore entityStore, EntityId entityId, CancellationToken cancellationToken = default) =>
        await entityStore.GetHistoryAsync(entityId, EntityVersion.Beginning, cancellationToken);


    /// <summary>Check if an entity id is taken.</summary>
    /// <param name="entityStore"></param>
    /// <param name="entityId">the id to verify</param>
    /// <returns>true if there is an entity with the given id, false otherwise</returns>
    public static bool ContainsEntity(this EntityStore entityStore, EntityId entityId) => entityStore.ContainsEntityAsync(entityId).Result;

    /// <summary>Check if an entity id is taken.</summary>
    /// <param name="entityStore"></param>
    /// <param name="entityId">the id to verify</param>
    /// <param name="cancellationToken"></param>
    /// <returns>true if there is an entity with the given id, false otherwise</returns>
    public static async Task<bool> ContainsEntityAsync(this EntityStore entityStore, EntityId entityId, CancellationToken cancellationToken = default) =>
        await entityStore.GetEntityTypeAsync(entityId, cancellationToken) is not null;

    /// <summary>Looks up the type of an entity. Useful for quickly checking if an entity id is taken.</summary>
    /// <param name="entityStore"></param>
    /// <param name="entityId">the id to verify</param>
    /// <returns>true if there is an entity with the given id, false otherwise</returns>
    public static EntityType? GetEntityType(this EntityStore entityStore, EntityId entityId) => entityStore.GetEntityTypeAsync(entityId).Result;

    private static TEntity RestoreEntity<TEntity>(this EntityStore entityStore, ISnapshot<TEntity> snapshot, EntityHistory history) where TEntity : class, IEntity
    {
        var entity = entityStore.Instantiate<TEntity>(snapshot.Id, history.Version);
        snapshot.Restore(entity);
        entity.ReplayEvents(history.Events);
        return entity;
    }

    private static TEntity Instantiate<TEntity>(this EntityStore entityStore, EntityId id, EntityVersion version) where TEntity : IEntity
    {
        var constructor = typeof(TEntity).GetConstructor(new[] { typeof(EntityId), typeof(EntityVersion) });
        if (constructor is null) throw new InvalidEntityException(typeof(TEntity));
        return (TEntity)constructor.Invoke(constructor.GetParameters().Length == 2 ? new object[] { id, version } : new object[] { id, version, entityStore });
    }

    /// <summary>An entity snapshot that was never made.</summary>
    /// All events will have to be replayed to reconstitute from this snapshot.
    private sealed class NeverSnapshot<TEntity>(EntityId id, EntityType entityType) : ISnapshot<TEntity> where TEntity : class, IEntity
    {
        public EntityId Id { get; } = id;
        public EntityType EntityType { get; } = entityType;
        public EntityVersion Version => EntityVersion.Beginning;

        public void Restore(TEntity entity) { } // Intentionally does nothing
    }
}
