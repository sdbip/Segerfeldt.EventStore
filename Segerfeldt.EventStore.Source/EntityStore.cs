using Segerfeldt.EventStore.Source.Internals;

using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source
{
    /// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
    public interface IEntityStore
    {
        /// <summary>Finds all the events, and the current version, of an entity. Everything needed to reconstitute its state.</summary>
        /// <param name="entityId">the id of the entity</param>
        /// <param name="afterVersion">only events that occurred after this version (and excluding this version)  will be returned. useful if you have a snapshot.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>the complete history of the entity</returns>
        Task<EntityHistory?> GetHistoryAsync(EntityId entityId, EntityVersion afterVersion, CancellationToken cancellationToken = default);

        /// <summary>Looks up the type of an entity. Useful for quickly checking if an entity id is taken.</summary>
        /// <param name="entityId">the id to verify</param>
        /// <param name="cancellationToken"></param>
        /// <returns>the type of the entity, or null</returns>
        Task<EntityType?> GetEntityTypeAsync(EntityId entityId, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc />
    public sealed class EntityStore : IEntityStore
    {
        private readonly IConnectionPool connectionPool;

        /// <summary>Initialize a new <see cref="EntityStore"/></summary>
        /// <param name="connectionPool">opens connections to the database that stores the state of entities as sequences of events</param>
        public EntityStore(IConnectionPool connectionPool) => this.connectionPool = connectionPool;

        /// <inheritdoc />
        public async Task<EntityHistory?> GetHistoryAsync(EntityId entityId, EntityVersion afterVersion, CancellationToken cancellationToken) =>
            await new GetHistoryOperation(entityId, afterVersion).ExecuteAsync(connectionPool.CreateConnection(), cancellationToken);

        /// <inheritdoc />
        public async Task<EntityType?> GetEntityTypeAsync(EntityId entityId, CancellationToken cancellationToken = default) =>
            await new LookupEntityTypeOperation(entityId).ExecuteAsync(connectionPool.CreateConnection(), cancellationToken);
    }
}
