using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals
{
    internal class LookupEntityTypeOperation
    {
        private readonly EntityId entityId;

        public LookupEntityTypeOperation(EntityId entityId)
        {
            this.entityId = entityId;
        }

        public async Task<EntityType?> ExecuteAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var command = connection.CreateCommand(
                "SELECT type FROM Entities WHERE id = @entityId",
                ("@entityId", entityId.ToString()));

            await connection.OpenAsync(cancellationToken);
            return await command.ExecuteScalarAsync(cancellationToken) is string type
                ? new EntityType(type) : null;
        }
    }
}
