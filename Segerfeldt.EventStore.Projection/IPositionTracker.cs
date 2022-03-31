namespace Segerfeldt.EventStore.Projection;

public interface IPositionTracker
{
    long? GetLastFinishedProjectionId();
    void OnProjectionStarting(long position);
    void OnProjectionFinished(long position);
}
