using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public interface IReceptacle
    {
        /// <summary>The names of the events accepted by this receptacle</summary>
        IEnumerable<string> AcceptedEvents { get; }

        /// <summary>Receives the event</summary>
        /// <param name="event">an <see cref="Event"/> in transmission</param>
        Task ReceiveAsync(Event @event);
    }
}
