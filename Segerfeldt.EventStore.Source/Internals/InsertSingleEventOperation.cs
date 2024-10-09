using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals;

internal sealed class InsertSingleEventOperation(UnpublishedEvent @event, EntityId entityId, EntityType type, string actor)
{
    private readonly UnpublishedEvent @event = @event;
    private readonly EntityId entityId = entityId;
    private readonly EntityType type = type;
    private readonly string actor = actor;

    public async Task<UpdatedStorePosition> ExecuteAsync(DbConnection connection)
    {
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        var activeOperation = new MyActiveOperation(transaction, this);

        try
        {
            var result = await activeOperation.RunAsync();
            await transaction.CommitAsync();
            await connection.CloseAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            await connection.CloseAsync();
            throw;
        }
    }

    private sealed class MyActiveOperation(DbTransaction transaction, InsertSingleEventOperation operation)
        : ActiveOperation(transaction, operation.actor)
    {
        private readonly DbTransaction transaction = transaction;
        private readonly InsertSingleEventOperation operation = operation;

        public async Task<UpdatedStorePosition> RunAsync()
        {
            var currentVersion = await GetCurrentVersionAsync();
            if (currentVersion.IsNew) await InsertEntityAsync(operation.entityId, operation.type, EntityVersion.Of(1));

            return await InsertEventsForEntities([new EntityData(operation.entityId, operation.type, currentVersion, [operation.@event])]);
        }

        private async Task<EntityVersion> GetCurrentVersionAsync()
        {
            using var command = transaction.CreateCommand("SELECT version FROM Entities WHERE id = @entityId");
            command.AddParameter("@entityId", operation.entityId.ToString());
            return await command.ExecuteScalarAsync() is int versionValue
                ? EntityVersion.Of(versionValue)
                : EntityVersion.New;
        }
    }
}
