using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public interface IProjection
    {
        IEnumerable<string> HandledEvents { get; }
        Task InvokeAsync(Event @event);
    }
}
