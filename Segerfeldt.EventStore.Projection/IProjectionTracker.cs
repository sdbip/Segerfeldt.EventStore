namespace Segerfeldt.EventStore.Projection;

public interface IProjectionTracker
{
    long? GetLastFinishedPosition();
    void OnProjectionStarting(long position);
    void OnProjectionFinished(long position);
}
