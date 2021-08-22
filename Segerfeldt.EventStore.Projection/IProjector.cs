using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public interface IProjector
    {
        IEnumerable<string> HandledEvents { get; }
        Task InvokeAsync(Event @event);
    }
}
