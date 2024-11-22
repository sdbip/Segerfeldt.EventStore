namespace Segerfeldt.EventStore.Projection.MySQL.Tests;

public delegate void ProjectionDelegate(Event @event);

/// <summary><see cref="IReceptacle"/> implementation that uses a delegate</summary>
/// <remarks>Initialize a new <see cref="DelegateReceptacle"/></remarks>
/// <param name="delegate">the delegate to call when events are notified</param>
/// <param name="handledEvents">the events this delegate handles</param>
public sealed class DelegateReceptacle(ProjectionDelegate @delegate, IEnumerable<string> handledEvents) : IReceptacle
{
    private readonly ProjectionDelegate @delegate = @delegate;

    public IEnumerable<string> AcceptedEvents { get; } = handledEvents;

    /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
    /// <param name="delegate">the delegate to call when events are notified</param>
    /// <param name="handledEvents">the events this delegate handles</param>
    public DelegateReceptacle(ProjectionDelegate @delegate, params string[] handledEvents) : this(@delegate, (IEnumerable<string>)handledEvents) { }

    public void Update(Event @event) => @delegate(@event);
}
