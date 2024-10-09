namespace Segerfeldt.EventStore.Projection;

/// <summary>Tracks the position of the Projection database</summary>
/// Use this to persist your position in an event store after an appication reboot.
/// Note: if any receptacle crashes during projection, your position will not be
/// updated. If some receptacles have been updated before the crash, they might be
/// corrupted when the position is replayed. You might be able to
public interface IProjectionTracker
{
    /// <summary>Reads the las position successfully handled by the projection receptacles</summary>
    long? GetLastFinishedPosition();

    /// <summary>Signals that projection will start emitting all events at the next position</summary>
    /// This might be a good place to BEGIN TRANSACTION. If the database
    /// <param name="position">the position of the emitted events</param>
    void OnProjectionStarting(long position);

    /// <summary>Signals that projection has completed emitting all events at the current position</summary>
    /// This might be a good place to COMMIT the changes
    /// <param name="position">the position of the emitted events</param>
    void OnProjectionFinished(long position);

    /// <summary>Signals that projection has completed emitting all events at the current position</summary>
    /// This might be a good place to ROLLBACK the changes
    /// <param name="position">the position of the emitted events</param>
    void OnProjectionError(long position);
}
