using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Projection;

/// <summary>A repository that contains the published events of the entities.</summary>
public interface IEventSourceRepository
{
    /// <summary>Gets new events sorted chronologically</summary>
    /// <param name="afterPosition">The last position to skip as it has already been processed.</param>
    IEnumerable<Event> GetEvents(long afterPosition, int maxCount);
}
