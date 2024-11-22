using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source;

public interface IProjectionRepository
{
    /// <summary>Implement to retrieve all events in the database (for any entity) after a specified position</summary>
    /// <param name="after">The last already processed position. No events before or at this position should be returned.</param>
    /// <param name="maxCount">The maximum number of events to return</param>
    /// <param name="cancellationToken">A cancellation token</param>
    Task<IEnumerable<EventDAO>> GetEventsAsync(long? after, int maxCount, CancellationToken cancellationToken);
}

public record EventDAO(string Name, JsonElement Details, EntityId EntityId, EntityType EntityType, Ordinal Ordinal, Position Position);
