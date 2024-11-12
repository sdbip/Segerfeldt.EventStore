using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection;

/// <summary>A repository that contains the published events of the entities.</summary>
public interface IEventSourceRepository
{
    /// <summary>Gets new events sorted chronologically</summary>
    /// <param name="afterPosition">The last position to skip as it has already been processed.</param>
    Task<IEnumerable<Event>> GetEventsAsync(long afterPosition, int maxCount);
}
