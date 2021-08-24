using Segerfeldt.EventStore.Source.Internals;
using Segerfeldt.EventStore.Source.Snapshots;

using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public sealed class EntityStore
    {
        private readonly DbConnection connection;

        /// <summary>Initialize a new <see cref="EntityStore"/></summary>
        /// <param name="connection">a connection to the database that stores the state of entities as sequences of events</param>
        public EntityStore(DbConnection connection)
        {
            this.connection = connection;
        }

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="id">the unique identifier of the entity to reconstitute</param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        /// <returns>the entity with the specified <paramref name="id"/></returns>
        public TEntity? Reconstitute<TEntity>(EntityId id) where TEntity : class, IEntity =>
            ReconstituteAsync<TEntity>(id).Result;

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="id">the unique identifier of the entity to reconstitute</param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        /// <returns>the entity with the specified <paramref name="id"/></returns>
        public async Task<TEntity?> ReconstituteAsync<TEntity>(EntityId id) where TEntity : class, IEntity =>
            await ReconstituteAsync(new NeverSnapshot<TEntity>(id));

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="snapshot">the snapshot of the entity</param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        public TEntity? Reconstitute<TEntity>(ISnapshot<TEntity> snapshot) where TEntity : class, IEntity =>
            ReconstituteAsync(snapshot).Result;

        /// <summary>Reconstitute the state of an entity from published events</summary>
        /// <param name="snapshot">the snapshot of the entity</param>
        /// <typeparam name="TEntity">the type of the entity</typeparam>
        public async Task<TEntity?> ReconstituteAsync<TEntity>(ISnapshot<TEntity> snapshot) where TEntity : class, IEntity =>
            await GetHistoryAsync(snapshot.Id, snapshot.Version) is { } history
                ? RestoreEntity(snapshot, history)
                : null;

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <returns>the complete history of the entity</returns>
        public EntityHistory? GetHistory(EntityId entityId) =>
            GetHistoryAsync(entityId).Result;

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <returns>the complete history of the entity</returns>
        public async Task<EntityHistory?> GetHistoryAsync(EntityId entityId) =>
            await GetHistoryAsync(entityId, EntityVersion.Beginning);

        private async Task<EntityHistory?> GetHistoryAsync(EntityId entityId, EntityVersion afterVersion) =>
            await new GetHistoryOperation(entityId, afterVersion).ExecuteAsync(connection);

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
            if (constructor is null) throw new Exception("Invalid entity type. Constructor missing.");
            return (TEntity)constructor.Invoke(new object[] { id, version });
        }

        /// <summary>An entity snapshot that was never made.</summary>
        /// All events will have to be replayed to reconstitute from this snapshot.
        private class NeverSnapshot<TEntity> : ISnapshot<TEntity> where TEntity : class, IEntity
        {
            public EntityId Id { get; }
            public EntityVersion Version => EntityVersion.Beginning;

            public NeverSnapshot(EntityId id) => Id = id;

            public void Restore(TEntity entity) { } // Intentionally does nothing
        }
    }
}
