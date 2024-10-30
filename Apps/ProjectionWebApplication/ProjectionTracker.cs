using Segerfeldt.EventStore.Projection;

namespace ProjectionWebApplication;

public sealed class ProjectionTracker(TargetDatabase connection) : IProjectionTracker
{
    private readonly TargetDatabase connection = connection;

    public long? Position { get; private set; }

    public long? GetLastFinishedPosition() => null;


    public void OnProjectionFinished(long position, Transaction transaction)
    {
        Position = position;
    }
}
