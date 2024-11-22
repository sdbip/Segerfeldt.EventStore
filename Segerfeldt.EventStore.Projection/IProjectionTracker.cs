using System;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection;

/// <summary>Tracks the position of the Projection database</summary>
/// Use this to persist the last received position in each <see cref="EventSource"/>
/// so that it can be prevented from repeating the same updates again.
///
/// Note: if any <see cref="Receptacle"/> crashes during its update, your position
/// will not be updated. If some receptacles have already been updated before the
/// crash, their changes should be rolled back as the events at that position will
/// all be emmitted again at the next opportunity.
public interface IProjectionTracker
{
    /// <summary>Reads the last position successfully handled by the projection receptacles</summary>
    long? GetLastFinishedPosition();

    /// <summary>Perform tasks (such as start/commit transaction) before and after emitting all events for the same position</summary>
    /// <param name="position">The position of the emitted events; store this when done</param>
    /// <param name="runProjection">This actually emits the events</param>
    Task ProjectingPosition(long position, Action runProjection);
}
