using Segerfeldt.EventStore.Projection;

namespace ProjectionWebApplication;

public class ProjectionTracker : IProjectionTracker
{
    public long? Position { get; private set; }

    public long? GetLastFinishedPosition() => null;

    public void OnProjectionStarting(long position) { }

    public void OnProjectionFinished(long position)
    {
        Position = position;
    }
}
