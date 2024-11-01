using Segerfeldt.EventStore.Shared;
using Segerfeldt.EventStore.Source.CommandAPI;

using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals;

public interface IEventPublisherRepository
{
    DbConnection CreateConnection();
    Task<int?> GetCurrentVersionAsync(EntityId entityId, DbTransaction transaction);
    Task<int?> GetHighestOrdinalAsync(EntityId entityId, DbTransaction transaction);
    Task<long?> GetLastPositionAsync(DbTransaction transaction);
    Task InsertEntityAsync(EntityId id, EntityType type, EntityVersion version, DbTransaction transaction);
    Task InsertEventAsync(EntityId entityId, UnpublishedEvent @event, string actor, EventOrdinal ordinal, long position, DbTransaction transaction);
    Task UpdateVersionAsync(EntityId id, EntityVersion version, DbTransaction transaction);
}

internal static class EventPublisherRepositoryExtensions
{
    public static async Task<EntityVersion> GetCurrentEntityVersionAsync(this IEventPublisherRepository repository, EntityId entityId, DbTransaction transaction)
    {
        var version = await repository.GetCurrentVersionAsync(entityId, transaction);
        return version.HasValue ? EntityVersion.Safe(version.Value) : EntityVersion.New;
    }

    public static async Task<EventOrdinal> GetNextOrdinalAsync(this IEventPublisherRepository repository, EntityId entityId, DbTransaction transaction)
    {
        var ordinal = await repository.GetHighestOrdinalAsync(entityId, transaction);
        return ordinal.HasValue ? EventOrdinal.Safe(ordinal.Value + 1) : EventOrdinal.Zero;
    }

    public static async Task<long> GetNextPositionAsync(this IEventPublisherRepository repository, DbTransaction transaction) =>
        await repository.GetLastPositionAsync(transaction) ?? 0;
}

public sealed class EventPublisherRepository(EventStoreConnectionFactory connectionFactory) : IEventPublisherRepository
{
    public DbConnection CreateConnection() => connectionFactory.CreateConnection();

    public async Task<int?> GetCurrentVersionAsync(EntityId entityId, DbTransaction transaction)
    {
        using var command = transaction.CreateCommand("SELECT version FROM Entities WHERE id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());
        var scalar = await command.ExecuteScalarAsync();
        return scalar is null ? null : Convert.ToInt32(scalar);
    }

    public async Task<int?> GetHighestOrdinalAsync(EntityId entityId, DbTransaction transaction)
    {
        var command = transaction.CreateCommand("SELECT max(ordinal) FROM Events WHERE entity_id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());
        var result = await command.ExecuteScalarAsync();
        return result is int v ? v : null;
    }

    public async Task<long?> GetLastPositionAsync(DbTransaction transaction)
    {
        var command = transaction.CreateCommand("SELECT max(position) FROM Events");
        var result = await command.ExecuteScalarAsync();
        return result as long?;
    }

    public async Task InsertEntityAsync(EntityId id, EntityType type, EntityVersion version, DbTransaction transaction)
    {
        using var command = transaction.CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@id, @type, @version)");
        command.AddParameter("@id", id.ToString());
        command.AddParameter("@type", type.ToString());
        command.AddParameter("@version", version.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateVersionAsync(EntityId id, EntityVersion version, DbTransaction transaction)
    {
        using var command = transaction.CreateCommand("UPDATE Entities SET version = @version WHERE id = @id");
        command.AddParameter("@id", id.ToString());
        command.AddParameter("@version", version.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task InsertEventAsync(EntityId entityId, UnpublishedEvent @event, string actor, EventOrdinal ordinal, long position, DbTransaction transaction)
    {
        using var command = transaction.CreateCommand(
            "INSERT INTO Events (entity_id, name, details, actor, ordinal, position)" +
            " VALUES (@entityId, @eventName, @details, @actor, @ordinal, @position)");
        command.AddParameter("@entityId", entityId.ToString());
        command.AddParameter("@eventName", @event.Name);
        command.AddParameter("@details", JSON.Serialize(@event.Details));
        command.AddParameter("@actor", actor);
        command.AddParameter("@ordinal", ordinal.Value);
        command.AddParameter("@position", position);
        await command.ExecuteNonQueryAsync();
    }
}
