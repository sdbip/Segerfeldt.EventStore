using Segerfeldt.EventStore.Projection;
using Segerfeldt.EventStore.Projection.Hosting;

namespace ProjectionWebApplication
{
    public class PositionTracker : IPositionTracker
    {
        public long? Position { get; private set; }

        public void UpdatePosition(object? sender, EventSource.EventsProcessedArgs args)
        {
            Position = args.Position;
        }
    }
}
