using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection;

public interface IReceptacle
{
    /// <summary>The names of the events accepted by this receptacle</summary>
    IEnumerable<string> AcceptedEvents { get; }

    /// <summary>Updates the receptacle with an event</summary>
    /// <param name="event">an emitted <see cref="Event"/></param>
    Task UpdateAsync(Event @event);
}
