using Segerfeldt.EventStore.Source.CommandAPI;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.MSSQL.CommandAPI;

/// <summary>A special implementation of <see cref="IProjectionRepository"/> for SQL Server.</summary>
/// The SQL standard is not a standard. Case in point: it does not support the LIMIT keyword
/// (which one might assume was universal). Much like IE in its hey-day, SQL Server is the
/// petulant child that requires special treatment.
internal class SQLServerProjectionRepository(EventStoreConnectionFactory connectionFactory) : IProjectionRepository
{
    public async Task<IEnumerable<EventDAO>> GetEventsAsync(long? after, int maxCount, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand($"""
            SELECT TOP {maxCount} Events.*, Entities.type AS entity_type FROM Events
                JOIN Entities ON Entities.id = Events.entity_id
                WHERE position > @position
            """);
        command.AddParameter("@after", after ?? -1);

        await connection.OpenAsync(cancellationToken);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.AllRowsAs(r => new EventDAO(
            Name: (string)r["name"],
            Details: JsonSerializer.Deserialize<JsonElement>((string)r["details"]),
            EntityId: EntityId.Value((string)r["entity_id"]),
            EntityType: EntityType.Name((string)r["entity_type"]),
            Ordinal: Ordinal.Of((int)r["ordinal"]),
            Position: Position.Of((long)r["position"])));
    }
}
