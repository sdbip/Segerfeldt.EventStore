using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals
{
    internal sealed class InsertEventsOperation
    {
        private readonly IEnumerable<IEntity> entities;
        private readonly string actor;

        public InsertEventsOperation(IEnumerable<IEntity> entities, string actor)
        {
            this.entities = entities;
            this.actor = actor;
        }

        public async Task ExecuteAsync(DbConnection connection)
        {
            await connection.OpenAsync();
            var transaction = await connection.BeginTransactionAsync();
            var activeOperation = new MyActiveOperation(transaction, this);

            try
            {
                await activeOperation.RunAsync();
                await transaction.CommitAsync();
                await connection.CloseAsync();
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
            private readonly InsertEventsOperation operation;

            public MyActiveOperation(DbTransaction transaction, InsertEventsOperation operation) :
                base(transaction, operation.actor)
            {
                this.operation = operation;
            }

            public async Task RunAsync()
            {
                foreach (var entity in operation.entities)
                {
                    var currentVersion = await GetCurrentVersionAsync(entity.Id);
                    if (entity.Version != currentVersion)
                        throw new ConcurrentUpdateException(entity.Version, currentVersion);

                    if (currentVersion.IsNew) await InsertEntityAsync(entity.Id, entity.Type, entity.Version);
                }

                foreach (var entity in operation.entities)
                    await InsertEventsAsync(entity.Id,
                        entity.UnpublishedEvents.Zip(InfiniteVersionsFrom(entity.Version.Next())));

                IEnumerable<EntityVersion> InfiniteVersionsFrom(EntityVersion first)
                {
                    var next = first;
                    while (true)
                    {
                        yield return next;
                        next = next.Next();
                    }
                    // ReSharper disable once IteratorNeverReturns
                }
            }

        }
    }
}
