using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public interface IReceptacle
    {
        /// <summary>The names of the events accepted by this receptacle</summary>
        IEnumerable<AcceptedEventName> AcceptedEventNames { get; }

        /// <summary>Receives the event</summary>
        /// <param name="event">an <see cref="Event"/> in transmission</param>
        Task ReceiveAsync(Event @event);

        /// <summary>Whether this receptacle is tuned to this event</summary>
        /// <param name="event">an <see cref="Event"/> in transmission</param>
        /// <returns><c>true</c> if the receptacle will accept the <paramref name="event"/>, <c>false</c> otherwise.</returns>
        bool Accepts(Event @event) => AcceptedEventNames.Any(e => e.IndicatesAcceptanceOf(@event.Name, @event.EntityType));
    }
}
