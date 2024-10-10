using Segerfeldt.EventStore.Source.Internals;

using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
public sealed class EntityStore
{
    private readonly IConnectionPool connectionPool;

    internal EntityStore(IConnectionPool connectionPool) => this.connectionPool = connectionPool;

    public EntityStore(DbConnection connection) : this(new OnDemandConnectionFactory(() => connection)) { }

    /// <summary>Finds all the events, and the current version, of an entity. Everything needed to reconstitute its state.</summary>
    /// <param name="entityId">the id of the entity</param>
    /// <param name="afterVersion">only events that occurred after this version (and excluding this version)  will be returned. useful if you have a snapshot.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>the complete history of the entity</returns>
    public async Task<EntityHistory?> GetHistoryAsync(EntityId entityId, EntityVersion afterVersion, CancellationToken cancellationToken = default) =>
        await new GetHistoryOperation(entityId, afterVersion).ExecuteAsync(connectionPool.CreateConnection(), cancellationToken);

    /// <summary>Looks up the type of an entity. Useful for quickly checking if an entity id is taken.</summary>
    /// <param name="entityId">the id to verify</param>
    /// <param name="cancellationToken"></param>
    /// <returns>the type of the entity, or null</returns>
    public async Task<EntityType?> GetEntityTypeAsync(EntityId entityId, CancellationToken cancellationToken = default) =>
        await new LookupEntityTypeOperation(entityId).ExecuteAsync(connectionPool.CreateConnection(), cancellationToken);
}
