using Segerfeldt.EventStore.Source.Internals;
using Segerfeldt.EventStore.Source.Snapshots;

using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public sealed class EntityStore
    {
        private readonly IConnectionPool connectionPool;

        /// <summary>Initialize a new <see cref="EntityStore"/></summary>
        /// <param name="connectionPool">opens connections to the database that stores the state of entities as sequences of events</param>
        public EntityStore(IConnectionPool connectionPool)
        {
            this.connectionPool = connectionPool;
        }

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="id">the unique identifier of the entity to reconstitute</param>
        /// <param name="type"></param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        /// <returns>the entity with the specified <paramref name="id"/></returns>
        public TEntity? Reconstitute<TEntity>(EntityId id, EntityType type) where TEntity : class, IEntity =>
            ReconstituteAsync<TEntity>(id, type).Result;

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="id">the unique identifier of the entity to reconstitute</param>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        /// <returns>the entity with the specified <paramref name="id"/></returns>
        public async Task<TEntity?> ReconstituteAsync<TEntity>(EntityId id, EntityType type, CancellationToken cancellationToken = default) where TEntity : class, IEntity =>
            await ReconstituteAsync(new NeverSnapshot<TEntity>(id, type), cancellationToken);

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="snapshot">the snapshot of the entity</param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        public TEntity? Reconstitute<TEntity>(ISnapshot<TEntity> snapshot) where TEntity : class, IEntity =>
            ReconstituteAsync(snapshot).Result;

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="snapshot">the snapshot of the entity</param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        public async Task<TEntity?> ReconstituteAsync<TEntity>(ISnapshot<TEntity> snapshot, CancellationToken cancellationToken = default) where TEntity : class, IEntity
        {
            var history = await GetHistoryAsync(snapshot.Id, snapshot.Version, cancellationToken);
            if (history is null) return snapshot.Version.IsNew ? null : throw new UnknownEntityException(snapshot.Id);
            if (history.Type != snapshot.EntityType) throw new IncorrectTypeException(snapshot.EntityType, history.Type);
            return RestoreEntity(snapshot, history);
        }

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <returns>the complete history of the entity</returns>
        public EntityHistory? GetHistory(EntityId entityId) =>
            GetHistoryAsync(entityId).Result;

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <param name="cancellationToken"></param>
        /// <returns>the complete history of the entity</returns>
        public async Task<EntityHistory?> GetHistoryAsync(EntityId entityId, CancellationToken cancellationToken = default) =>
            await GetHistoryAsync(entityId, EntityVersion.Beginning, cancellationToken);

        private async Task<EntityHistory?> GetHistoryAsync(EntityId entityId, EntityVersion afterVersion, CancellationToken cancellationToken) =>
            await new GetHistoryOperation(entityId, afterVersion).ExecuteAsync(connectionPool.CreateConnection(), cancellationToken);

        /// <summary>Whether there exists an entity with a specific id</summary>
        /// <param name="entityId">the id to verify</param>
        /// <param name="entityType">the expected type of the entity</param>
        /// <returns>true if there is an entity with the given id, false otherwise</returns>
        public bool ContainsEntity(EntityId entityId, EntityType? entityType = null) => ContainsEntityAsync(entityId, entityType).Result;

        /// <summary>Whether there exists an entity with a specific id</summary>
        /// <param name="entityId">the id to verify</param>
        /// <param name="entityType">the expected type of the entity</param>
        /// <param name="cancellationToken"></param>
        /// <returns>true if there is an entity with the given id, false otherwise</returns>
        public async Task<bool> ContainsEntityAsync(EntityId entityId, EntityType? entityType = null, CancellationToken cancellationToken = default) =>
            await new ContainsEntityOperation(entityId, entityType).ExecuteAsync(connectionPool.CreateConnection(), cancellationToken);

        private static TEntity RestoreEntity<TEntity>(ISnapshot<TEntity> snapshot, EntityHistory history) where TEntity : class, IEntity
        {
            var entity = Instantiate<TEntity>(snapshot.Id, history.Version);
            snapshot.Restore(entity);
            entity.ReplayEvents(history.Events);
            return entity;
        }

        private static TEntity Instantiate<TEntity>(EntityId id, EntityVersion version) where TEntity : IEntity
        {
            var constructor = typeof(TEntity).GetConstructor(new[] { typeof(EntityId), typeof(EntityVersion) });
            if (constructor is null) throw new InvalidEntityException(typeof(TEntity));
            return (TEntity)constructor.Invoke(new object[] { id, version });
        }

        /// <summary>An entity snapshot that was never made.</summary>
        /// All events will have to be replayed to reconstitute from this snapshot.
        private class NeverSnapshot<TEntity> : ISnapshot<TEntity> where TEntity : class, IEntity
        {
            public EntityId Id { get; }
            public EntityType EntityType { get; }
            public EntityVersion Version => EntityVersion.Beginning;

            public NeverSnapshot(EntityId id, EntityType entityType)
            {
                Id = id;
                EntityType = entityType;
            }

            public void Restore(TEntity entity) { } // Intentionally does nothing
        }
    }
}
