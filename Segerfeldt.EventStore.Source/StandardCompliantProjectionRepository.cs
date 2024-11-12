using Segerfeldt.EventStore.Source.CommandAPI;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

/// <summary>The default implementation of <see cref="IProjectionRepository"/>. This should be adequate for reasonable database providers.</summary>
/// <param name="connectionFactory"></param>
public class StandardCompliantProjectionRepository(EventStoreConnectionFactory connectionFactory) : IProjectionRepository
{
    public async Task<IEnumerable<EventDAO>> GetEventsAsync(long? after, int maxCount, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await using var command = connection.CreateCommand("""
            SELECT Events.*, Entities.type AS entity_type FROM Events
                JOIN Entities ON Entities.id = Events.entity_id
                WHERE position > @after
            LIMIT @maxCount
            """);
        command.AddParameter("@after", after ?? -1);
        command.AddParameter("@maxCount", maxCount);

        await connection.OpenAsync(cancellationToken);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        return reader.AllRowsAs(r => new EventDAO(
            Name: (string)r["name"],
            Details: JsonSerializer.Deserialize<IDictionary<string, object>>((string)r["details"])!,
            EntityId: EntityId.Value((string)r["entity_id"]),
            EntityType: EntityType.Name((string)r["entity_type"]),
            Ordinal: EventOrdinal.Of(r.GetInt32(r.GetOrdinal("ordinal"))),
            Position: Position.Of(r.GetInt64(r.GetOrdinal("position")))));
    }
}
