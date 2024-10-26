using Segerfeldt.EventStore.Refactoring;

namespace RefactoringWebApplication;

/// <inheritdoc/>
public sealed class ProjectionTracker : IProjectionTracker
{
    internal long? Position { get; private set; }

    /// <inheritdoc/>
    public long? GetLastFinishedPosition() => null;

    /// <inheritdoc/>
    public void OnProjectionStarting(long position) { }

    /// <inheritdoc/>
    public void OnProjectionFinished(long position)
    {
        Position = position;
    }

    /// <inheritdoc/>
    public void OnProjectionError(long position) { }
}
