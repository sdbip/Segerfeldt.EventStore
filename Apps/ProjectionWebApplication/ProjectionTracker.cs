using Segerfeldt.EventStore.Projection;

using System.Data;

namespace ProjectionWebApplication;

public sealed class ProjectionTracker(TargetDatabase connection) : IProjectionTracker
{
    private readonly TargetDatabase connection = connection;

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
