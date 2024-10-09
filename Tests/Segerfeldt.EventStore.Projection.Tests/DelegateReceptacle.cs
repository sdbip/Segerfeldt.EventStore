using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.Tests;

public delegate Task AsyncProjectionDelegate(Event @event);
public delegate void ProjectionDelegate(Event @event);

/// <summary><see cref="IReceptacle"/> implementation that uses a delegate</summary>
public sealed class DelegateReceptacle : IReceptacle
{
    private readonly Delegate @delegate;

    public IEnumerable<string> AcceptedEvents { get; }

    /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
    /// <param name="delegate">the delegate to call when events are notified</param>
    /// <param name="handledEvents">the events this delegate handles</param>
    public DelegateReceptacle(AsyncProjectionDelegate @delegate, params string[] handledEvents) : this(@delegate, (IEnumerable<string>)handledEvents) { }

    /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
    /// <param name="delegate">the delegate to call when events are notified</param>
    /// <param name="handledEvents">the events this delegate handles</param>
    public DelegateReceptacle(AsyncProjectionDelegate @delegate, IEnumerable<string> handledEvents)
    {
        AcceptedEvents = handledEvents;
        this.@delegate = @delegate;
    }

    /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
    /// <param name="delegate">the delegate to call when events are notified</param>
    /// <param name="handledEvents">the events this delegate handles</param>
    public DelegateReceptacle(ProjectionDelegate @delegate, params string[] handledEvents) : this(@delegate, (IEnumerable<string>)handledEvents) { }

    /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
    /// <param name="delegate">the delegate to call when events are notified</param>
    /// <param name="handledEvents">the events this delegate handles</param>
    public DelegateReceptacle(ProjectionDelegate @delegate, IEnumerable<string> handledEvents)
    {
        AcceptedEvents = handledEvents;
        this.@delegate = @delegate;
    }

    public Task UpdateAsync(Event @event) => @delegate.DynamicInvoke(@event) as Task ?? Task.CompletedTask;
}
