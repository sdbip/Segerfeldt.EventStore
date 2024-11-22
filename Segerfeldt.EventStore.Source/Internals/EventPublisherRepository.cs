using Segerfeldt.EventStore.Source.CommandAPI;

using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals;

public interface IEventPublisherRepository
{
    DbConnection CreateConnection();
    Task<IAtomicOperation> BeginAtomicOperationAsync();
    IAtomicOperation CreateOperation(IDbTransaction transaction);

    Task<int?> GetCurrentVersionAsync(EntityId entityId, IAtomicOperation operation);
    Task<int?> GetHighestOrdinalAsync(EntityId entityId, IAtomicOperation operation);
    Task<long?> GetLastPositionAsync(IAtomicOperation operation);

    Task InsertEntityAsync(EntityId id, EntityType type, EntityVersion version, IAtomicOperation operation);
    Task InsertEventAsync(EntityId entityId, UnpublishedEvent @event, string actor, Ordinal ordinal, Position position, IAtomicOperation operation);
    Task UpdateVersionAsync(EntityId id, EntityVersion version, IAtomicOperation operation);
}

public interface IAtomicOperation
{
    Task AbortAsync();
    Task CommitAsync();
}

public sealed class EventPublisherRepository(EventStoreConnectionFactory connectionFactory) : IEventPublisherRepository
{
    public DbConnection CreateConnection() => connectionFactory.CreateConnection();
    public async Task<IAtomicOperation> BeginAtomicOperationAsync()
    {
        var connection = CreateConnection();
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        return new Transaction(connection, transaction);
    }
    public IAtomicOperation CreateOperation(IDbTransaction transaction) =>
        new Transaction((DbConnection)transaction.Connection!, (DbTransaction)transaction);

    public async Task<int?> GetCurrentVersionAsync(EntityId entityId, IAtomicOperation operation)
    {
        using var command = GetDbTransaction(operation).CreateCommand("SELECT version FROM Entities WHERE id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());
        var scalar = await command.ExecuteScalarAsync();
        return IsNullResult(scalar) ? null : Convert.ToInt32(scalar);
    }

    public async Task<int?> GetHighestOrdinalAsync(EntityId entityId, IAtomicOperation operation)
    {
        var command = GetDbTransaction(operation).CreateCommand("SELECT max(ordinal) FROM Events WHERE entity_id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());
        var result = await command.ExecuteScalarAsync();
        return IsNullResult(result) ? null : Convert.ToInt32(result);
    }

    public async Task<long?> GetLastPositionAsync(IAtomicOperation operation)
    {
        var command = GetDbTransaction(operation).CreateCommand("SELECT max(position) FROM Events");
        var result = await command.ExecuteScalarAsync();
        return IsNullResult(result) ? null : Convert.ToInt64(result);
    }

    public async Task InsertEntityAsync(EntityId id, EntityType type, EntityVersion version, IAtomicOperation operation)
    {
        using var command = GetDbTransaction(operation).CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@id, @type, @version)");
        command.AddParameter("@id", id.ToString());
        command.AddParameter("@type", type.ToString());
        command.AddParameter("@version", version.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateVersionAsync(EntityId id, EntityVersion version, IAtomicOperation operation)
    {
        using var command = GetDbTransaction(operation).CreateCommand("UPDATE Entities SET version = @version WHERE id = @id");
        command.AddParameter("@id", id.ToString());
        command.AddParameter("@version", version.Value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task InsertEventAsync(EntityId entityId, UnpublishedEvent @event, string actor, Ordinal ordinal, Position position, IAtomicOperation operation)
    {
        using var command = GetDbTransaction(operation).CreateCommand(
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

    private static DbTransaction GetDbTransaction(IAtomicOperation operation)
    {
        if (operation is not Transaction transaction) throw new ArgumentException($"Incompatible IUnitOfWork implementation class {operation.GetType()}", nameof(operation));
        return transaction.DbTransaction;
    }

    public class Transaction(DbConnection connection, DbTransaction transaction) : IAtomicOperation
    {
        private readonly DbConnection connection = connection;
        public DbTransaction DbTransaction { get; } = transaction;

        public async Task AbortAsync()
        {
            await DbTransaction.RollbackAsync();
            await connection.CloseAsync();
        }

        public async Task CommitAsync()
        {
            await DbTransaction.CommitAsync();
            await connection.CloseAsync();
        }
    }

    private static bool IsNullResult(object? o) => o is null or DBNull;
}
