using Segerfeldt.EventStore.Source.Internals;

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
        public async Task<TEntity?> ReconstituteAsync<TEntity>(EntityId id) where TEntity : class, IEntity
        {
            var operation = new GetHistoryOperation(id);
            var history = await operation.ExecuteAsync(connection);
            if (history is null) return null;

            var entity = Instantiate<TEntity>(id, history.Version);
            entity.ReplayEvents(history.Events);
            return entity;
        }

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <returns>the complete history of the entity</returns>
        public EntityHistory? GetHistory(EntityId entityId) =>
            GetHistoryAsync(entityId).Result;

        /// <summary>Get the historical data about an entity</summary>
        /// <param name="entityId">the unique identifier of the entity to reconstitute</param>
        /// <returns>the complete history of the entity</returns>
        public async Task<EntityHistory?> GetHistoryAsync(EntityId entityId)
        {
            var operation = new GetHistoryOperation(entityId);
            return await operation.ExecuteAsync(connection);
        }

        private static TEntity Instantiate<TEntity>(EntityId id, EntityVersion version) where TEntity : IEntity
        {
            var constructor = typeof(TEntity).GetConstructor(new[] { typeof(EntityId), typeof(EntityVersion) });
            if (constructor is null) throw new Exception("Invalid entity type. Constructor missing.");
            return (TEntity)constructor.Invoke(new object[] { id, version });
        }
    }
}
