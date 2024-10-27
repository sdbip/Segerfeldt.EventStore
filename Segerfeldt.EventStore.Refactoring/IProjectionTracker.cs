namespace Segerfeldt.EventStore.Refactoring;

/// <summary>Tracks the position of the Projection database</summary>
/// Use this to persist the position of the <see cref="EventSource"/>
/// so that it can be prevented from repeating the same updates again.
public interface IProjectionTracker
{
    /// <summary>Reads the last position successfully handled by the projection receptacles</summary>
    long? GetLastFinishedPosition();

    /// <summary>Signals that projection has completed emitting all events at the current position</summary>
    /// This would be a good place to COMMIT the transacion if you have one.
    /// <param name="position">the position of the last emitted events</param>
    void OnProjectionFinished(long position);
}
