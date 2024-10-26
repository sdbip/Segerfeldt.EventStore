using System.Collections.Generic;

namespace Segerfeldt.EventStore.Projection;

public interface IReceptacle
{
    /// <summary>The names of the events accepted by this receptacle</summary>
    IEnumerable<string> AcceptedEvents { get; }

    /// <summary>Updates the receptacle with an event</summary>
    /// <param name="event">an emitted <see cref="Event"/></param>
    void Update(Event @event);
}
