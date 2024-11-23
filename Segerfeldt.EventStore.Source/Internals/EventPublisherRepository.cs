using Segerfeldt.EventStore.Source.CommandAPI;

using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals;

public interface IEventPublisherRepository
{
    DbTransaction BeginTransaction();

    Task<int?> GetCurrentVersionAsync(EntityId entityId, DbTransaction transaction);
    Task<int?> GetHighestOrdinalAsync(EntityId entityId, DbTransaction transaction);
    Task<long?> GetLastPositionAsync(DbTransaction transaction);

    Task InsertEntityAsync(EntityId id, EntityType type, Ordinal version, DbTransaction transaction);
    Task InsertEventAsync(EntityId entityId, UnpublishedEvent @event, string actor, Ordinal ordinal, Position position, DbTransaction transaction);
    Task UpdateVersionAsync(EntityId id, Ordinal version, DbTransaction transaction);
}

public sealed class EventPublisherRepository(EventStoreConnectionFactory connectionFactory) : IEventPublisherRepository
{
    public DbTransaction BeginTransaction()
    {
        var connection = connectionFactory.CreateConnection();
        connection.Open();
        return connection.BeginTransaction();
    }

    public async Task<int?> GetCurrentVersionAsync(EntityId entityId, DbTransaction transaction)
    {
        using var command = transaction.CreateCommand("SELECT version FROM Entities WHERE id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());
        var scalar = await command.ExecuteScalarAsync();
        return IsNullResult(scalar) ? null : Convert.ToInt32(scalar);
    }

    public async Task<int?> GetHighestOrdinalAsync(EntityId entityId, DbTransaction transaction)
    {
        var command = transaction.CreateCommand("SELECT max(ordinal) FROM Events WHERE entity_id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());
        var result = await command.ExecuteScalarAsync();
        return IsNullResult(result) ? null : Convert.ToInt32(result);
    }

    public async Task<long?> GetLastPositionAsync(DbTransaction transaction)
    {
        var command = transaction.CreateCommand("SELECT max(position) FROM Events");
        var result = await command.ExecuteScalarAsync();
        return IsNullResult(result) ? null : Convert.ToInt64(result);
    }

    public async Task InsertEntityAsync(EntityId id, EntityType type, Ordinal version, DbTransaction transaction)
    {
        using var command = transaction.CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@id, @type, @version)");
        command.AddParameter("@id", id.ToString());
        command.AddParameter("@type", type.ToString());
        command.AddParameter("@version", version.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateVersionAsync(EntityId id, Ordinal version, DbTransaction transaction)
    {
        using var command = transaction.CreateCommand("UPDATE Entities SET version = @version WHERE id = @id");
        command.AddParameter("@id", id.ToString());
        command.AddParameter("@version", version.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task InsertEventAsync(EntityId entityId, UnpublishedEvent @event, string actor, Ordinal ordinal, Position position, DbTransaction transaction)
    {
        using var command = transaction.CreateCommand(
            "INSERT INTO Events (entity_id, name, details, actor, ordinal, position)" +
            " VALUES (@entityId, @eventName, @details, @actor, @ordinal, @position)");
        command.AddParameter("@entityId", entityId.ToString());
        command.AddParameter("@eventName", @event.Name);
        command.AddParameter("@details", JSON.Serialize(@event.Details));
        command.AddParameter("@actor", actor);
        command.AddParameter("@ordinal", ordinal.Value);
        command.AddParameter("@position", position.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static bool IsNullResult(object? o) => o is null or DBNull;
}
