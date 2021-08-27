using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public interface IProjector
    {
        /// <summary>The names of the events handled by this projector</summary>
        IEnumerable<string> HandledEvents { get; }

        /// <summary>Invokes the method</summary>
        /// <param name="event">The name of the events that trigger this projection</param>
        Task InvokeAsync(Event @event);
    }
}
