using Segerfeldt.EventStore.Projection;

namespace ProjectionWebApplication;

public sealed class ProjectionTracker : IProjectionTracker
{
    public long? Position { get; private set; }

    public long? GetLastFinishedPosition() => null;

    public void OnProjectionStarting(long position) { }

    public void OnProjectionFinished(long position)
    {
        Position = position;
    }

    public void OnProjectionError(long position)
    {
        throw new System.NotImplementedException();
    }
}
