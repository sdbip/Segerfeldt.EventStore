using Segerfeldt.EventStore.Source.Snapshots;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

public sealed class InvalidEntityException(Type entityType) : Exception($"Invalid entity type {entityType.Name}. Constructor missing.") { }
public sealed class UnknownEntityException(EntityId entityId) : Exception($"No entity with the id '{entityId}' exists.") { }

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
public sealed class IncorrectTypeException(EntityType expectedType, EntityType actualType)
    : Exception($"Entity has the wrong type. Actual type is {actualType}, expected {expectedType}")
{
    public EntityType ExpectedType { get; } = expectedType;
    public EntityType ActualType { get; } = actualType;
}

public static class EntityStoreMethods
{
    /// <summary>Reconstitute the state of an entity from published events</summary>
    /// <param name="entityStore"></param>
    /// <param name="id">the unique identifier of the entity to reconstitute</param>
    /// <param name="type"></param>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    /// <returns>the entity with the specified <paramref name="id"/></returns>
    public static TEntity? Reconstitute<TEntity>(this EntityStore entityStore, TypedEntityId id, IDbTransaction? transaction = null) where TEntity : class, IEntity =>
        entityStore.ReconstituteAsync<TEntity>(id, transaction).Result;

    /// <summary>Reconstitute the state of an entity from published events</summary>
    /// <param name="entityStore"></param>
    /// <param name="id">the unique identifier of the entity to reconstitute</param>
    /// <param name="type"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    /// <returns>the entity with the specified <paramref name="id"/></returns>
    public static async Task<TEntity?> ReconstituteAsync<TEntity>(this EntityStore entityStore, TypedEntityId id, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where TEntity : class, IEntity =>
        await entityStore.ReconstituteAsync(new NeverSnapshot<TEntity>(id.Value, id.Type), transaction, cancellationToken);

    /// <summary>Reconstitute the state of an entity from published events</summary>
    /// <param name="entityStore"></param>
    /// <param name="snapshot">the snapshot of the entity</param>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    public static TEntity? Reconstitute<TEntity>(this EntityStore entityStore, ISnapshot<TEntity> snapshot, IDbTransaction? transaction = null) where TEntity : class, IEntity =>
        entityStore.ReconstituteAsync(snapshot, transaction).Result;

    /// <summary>Reconstitute the state of an entity from published events</summary>
    /// <param name="entityStore"></param>
    /// <param name="snapshot">the snapshot of the entity</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEntity">the type of the entity</typeparam>
    public static async Task<TEntity?> ReconstituteAsync<TEntity>(this EntityStore entityStore, ISnapshot<TEntity> snapshot, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where TEntity : class, IEntity =>
        await ReconstituteAsync(entityStore, SnapshotRestorer<TEntity>.Applying(snapshot), transaction, cancellationToken);

    private static async Task<TEntity?> ReconstituteAsync<TEntity>(this EntityStore entityStore, SnapshotRestorer<TEntity> snapshot, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where TEntity : class, IEntity
    {
        var history = await entityStore.GetHistoryAsync(snapshot.Id, snapshot.Ordinal, transaction, cancellationToken);
        if (history is null) return snapshot.Ordinal is null ? null : throw new UnknownEntityException(snapshot.Id);
        if (history.Type != snapshot.EntityType) throw new IncorrectTypeException(snapshot.EntityType, history.Type);
        return entityStore.RestoreEntity(snapshot, history);
    }

    /// <summary>Get the historical data about an entity</summary>
    /// <param name="entityStore"></param>
    /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
    /// <returns>the complete history of the entity</returns>
    public static EntityHistory? GetHistory(this EntityStore entityStore, EntityId entityId, EventOrdinal? after = null, IDbTransaction? transaction = null) =>
        entityStore.GetHistoryAsync(entityId, after, transaction).Result;


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

    private static TEntity RestoreEntity<TEntity>(this EntityStore entityStore, SnapshotRestorer<TEntity> snapshot, EntityHistory history) where TEntity : class, IEntity
    {
        var entity = entityStore.Instantiate<TEntity>(snapshot.Id, history.Version);
        snapshot.Restore(entity);
        entity.ReplayEvents(history.Events);
        return entity;
    }

    private static TEntity Instantiate<TEntity>(this EntityStore entityStore, EntityId id, EntityVersion version) where TEntity : IEntity
    {
        var constructor = typeof(TEntity).GetConstructor([typeof(EntityId), typeof(EntityVersion)])
            ?? throw new InvalidEntityException(typeof(TEntity));
        return (TEntity)constructor.Invoke(constructor.GetParameters().Length == 2
            ? [id, version]
            : [id, version, entityStore]);
    }

    private class SnapshotRestorer<TEntity>(EntityId id, EntityType entityType, EventOrdinal? ordinal, Action<TEntity> restore) where TEntity : class, IEntity
    {
        public EntityId Id { get; } = id;
        public EntityType EntityType { get; } = entityType;
        public EventOrdinal? Ordinal => ordinal;

        public static SnapshotRestorer<TEntity> Applying(ISnapshot<TEntity> snapshot) => new(snapshot.Id, snapshot.EntityType, snapshot.Ordinal, snapshot.Restore);

        internal void Restore(TEntity entity) { restore(entity); }
    }

    /// <summary>An entity snapshot that was never made.</summary>
    /// All events will have to be replayed to reconstitute from this snapshot.
    private sealed class NeverSnapshot<TEntity>(EntityId id, EntityType entityType)
        : SnapshotRestorer<TEntity>(id, entityType, null, _ => {}) where TEntity : class, IEntity { }
}
