using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals;

internal sealed class InsertMultipleEventsOperation(IEnumerable<IEntity> entities, string actor)
{
    private readonly IEnumerable<IEntity> entities = entities.Where(e => e.UnpublishedEvents.Any());
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

    private sealed class MyActiveOperation(DbTransaction transaction, InsertMultipleEventsOperation operation)
        : ActiveOperation(transaction, operation.actor)
    {
        private readonly InsertMultipleEventsOperation operation = operation;

        public async Task<UpdatedStorePosition> RunAsync()
        {
            foreach (var entity in operation.entities)
            {
                var currentVersion = await GetCurrentVersionAsync(entity.Id);
                if (entity.Version != currentVersion)
                    throw new ConcurrentUpdateException(entity.Version, currentVersion);

                if (currentVersion.IsNew) await InsertEntityAsync(entity.Id, entity.Type, entity.Version);
            }

            return await InsertEventsForEntities(operation.entities.Select(e => new EntityData(e.Id, e.Type, e.Version, e.UnpublishedEvents)));
        }
    }
}
