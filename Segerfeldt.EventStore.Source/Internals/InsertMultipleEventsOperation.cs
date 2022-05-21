using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals;

internal sealed class InsertMultipleEventsOperation
{
    private readonly IEnumerable<IEntity> entities;
    private readonly string actor;

    public InsertMultipleEventsOperation(IEnumerable<IEntity> entities, string actor)
    {
        this.entities = entities.Where(e => e.UnpublishedEvents.Any());
        this.actor = actor;
    }

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

    private sealed class MyActiveOperation : ActiveOperation
    {
        private readonly InsertMultipleEventsOperation operation;

        public MyActiveOperation(DbTransaction transaction, InsertMultipleEventsOperation operation) :
            base(transaction, operation.actor) => this.operation = operation;

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
