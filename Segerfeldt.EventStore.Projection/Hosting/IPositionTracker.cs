namespace Segerfeldt.EventStore.Projection.Hosting
{
    public interface IPositionTracker
    {
        void UpdatePosition(object? sender, EventSource.EventsProcessedArgs args);
    }
}
