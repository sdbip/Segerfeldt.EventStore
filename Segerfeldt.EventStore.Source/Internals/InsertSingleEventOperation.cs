using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals
{
    internal sealed class InsertSingleEventOperation
    {
        private readonly UnpublishedEvent @event;
        private readonly EntityId entityId;
        private readonly EntityType type;
        private readonly string actor;

        public InsertSingleEventOperation(UnpublishedEvent @event, EntityId entityId, EntityType type, string actor)
        {
            this.@event = @event;
            this.entityId = entityId;
            this.type = type;
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
            private readonly DbTransaction transaction;
            private readonly InsertSingleEventOperation operation;

            public MyActiveOperation(DbTransaction transaction, InsertSingleEventOperation operation) : base(transaction, operation.actor)
            {
                this.transaction = transaction;
                this.operation = operation;
            }

            public async Task<UpdatedStorePosition> RunAsync()
            {
                var currentVersion = await GetCurrentVersionAsync();
                if (currentVersion.IsNew) await InsertEntityAsync(operation.entityId, operation.type, EntityVersion.Of(1));

                return await InsertEventsForEntities(new[] { new EntityData(operation.entityId, currentVersion, new[] { operation.@event }.AsEnumerable()) });
            }

            private async Task<EntityVersion> GetCurrentVersionAsync()
            {
                var command = transaction.CreateCommand("SELECT version FROM Entities WHERE id = @entityId",
                    ("@entityId", operation.entityId.ToString()));
                return await command.ExecuteScalarAsync() is int versionValue
                    ? EntityVersion.Of(versionValue)
                    : EntityVersion.New;
            }
        }
    }
}
