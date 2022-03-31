using Segerfeldt.EventStore.Projection;

namespace ProjectionWebApplication;

public class PositionTracker : IPositionTracker
{
    public long? Position { get; private set; }

    public long? GetLastFinishedProjectionId() => null;

    public void OnProjectionStarting(long position) { }

    public void OnProjectionFinished(long position)
    {
        Position = position;
    }
}
