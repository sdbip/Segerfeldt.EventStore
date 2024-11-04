using Segerfeldt.EventStore.Source.CommandAPI;
using Segerfeldt.EventStore.Source.Internals;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
public sealed class EntityStore(IEntityStoreRepository repository)
{
    private readonly IEntityStoreRepository repository = repository;

    public EntityStore(EventStoreConnectionFactory connectionFactory)
        : this(new EntityStoreRepository(connectionFactory)) { }

    public EntityStore(DbConnection connection) : this(EventStoreConnectionFactory.Singleton(connection)) { }

    /// <summary>Finds all the events, and the current version, of an entity. Everything needed to reconstitute its state.</summary>
    /// <param name="entityId">the id of the entity</param>
    /// <param name="after">An ordinal where only events that occurred after this point (and excluding this version)  will be returned. useful if you have a snapshot.</param>
    /// <param name="transaction">An active transaction (used when projecting) during which the requested entity might have been inserted</param>
    /// <param name="cancellationToken"></param>
    /// <returns>the complete history of the entity</returns>
    public async Task<EntityHistory?> GetHistoryAsync(EntityId entityId, EventOrdinal? after = null, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var nullableDAO = await repository.GetHistoryAsync(entityId, after, transaction as DbTransaction, cancellationToken);
        if (!nullableDAO.HasValue) return null;
        var dao = nullableDAO.Value;
        return new EntityHistory(
            EntityType.Safe(dao.Type),
            EntityVersion.Safe(dao.Version),
            dao.Events
                .Order(GenericComparer.Create<PublishedEventDAO>((e1, e2) => e1.Ordinal.CompareTo(e2.Ordinal)))
                .Select(e => new PublishedEvent(e.Name, e.Details, e.Actor, Timestamp.DaysSinceUnixEpoch(e.Timestamp))));
    }

    /// <summary>Looks up the type of an entity. Useful for quickly checking if an entity id is taken.</summary>
    /// <param name="entityId">the id to verify</param>
    /// <param name="cancellationToken"></param>
    /// <returns>the type of the entity, or null</returns>
    public async Task<EntityType?> GetEntityTypeAsync(EntityId entityId, CancellationToken cancellationToken = default)
    {
        var type = await repository.GetTypeAsync(entityId, cancellationToken);
        return type is null ? null : EntityType.Safe(type);
    }
}

public class GenericComparer<T>(Func<T?, T?, int> compare) : IComparer<T>
{
    public int Compare(T? x, T? y) => compare(x, y);
}

public static class GenericComparer
{
    public static GenericComparer<T> Create<T>() where T : IComparable => Create((T? x, T? y) =>
    {
        if (x != null) return x.CompareTo(y);
        if (y != null) return y.CompareTo(x);
        return 0;
    });

    internal static GenericComparer<T> Create<T>(Func<T?, T?, int> compare)
    {
        return new GenericComparer<T>(compare);
    }
}
