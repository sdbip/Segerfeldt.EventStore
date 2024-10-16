using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Source.Internals;

internal sealed class LookupEntityTypeOperation(EntityId entityId)
{
    private readonly EntityId entityId = entityId;

    public async Task<EntityType?> ExecuteAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand("SELECT type FROM Entities WHERE id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());

        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteScalarAsync(cancellationToken) is string type
            ? new EntityType(type) : null;
    }
}
