using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public interface IProjector
    {
        /// <summary>The names of the events handled by this projector</summary>
        IEnumerable<EventName> HandledEvents { get; }

        /// <summary>Invokes the method</summary>
        /// <param name="event">The name of the events that trigger this projection</param>
        Task InvokeAsync(Event @event);

        bool HandlesEvent(Event @event) => HandledEvents.Any(e => e.Handles(@event.Name));
    }
}
