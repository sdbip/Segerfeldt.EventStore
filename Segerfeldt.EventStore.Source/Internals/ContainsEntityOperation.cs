using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals
{
    internal class ContainsEntityOperation
    {
        private readonly EntityId entityId;
        private readonly EntityType? entityType;

        public ContainsEntityOperation(EntityId entityId, EntityType? entityType)
        {
            this.entityId = entityId;
            this.entityType = entityType;
        }

        public async Task<bool> ExecuteAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var command = entityType is null
                ? connection.CreateCommand(
                    "SELECT COUNT(*) FROM Entities WHERE id = @entityId",
                ("@entityId", entityId.ToString()))
                : connection.CreateCommand(
                    "SELECT COUNT(*) FROM Entities WHERE id = @entityId AND type = @entityType",
                    ("@entityId", entityId.ToString()),
                    ("@entityType", entityType.ToString()));

            await connection.OpenAsync(cancellationToken);
            return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
        }
    }
}
