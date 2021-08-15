using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public delegate Task AsyncProjectionDelegate(Event @event);
    public delegate void ProjectionDelegate(Event @event);

    /// <summary>IProjection implementation that uses a delegate</summary>
    public class DelegateProjection : IProjection
    {
        private readonly Delegate @delegate;

        public IEnumerable<string> HandledEvents { get; }

        /// <summary>Initialize a new <see cref="DelegateProjection"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateProjection(AsyncProjectionDelegate @delegate, params string[] handledEvents) : this(@delegate, (IEnumerable<string>)handledEvents) { }

        /// <summary>Initialize a new <see cref="DelegateProjection"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateProjection(AsyncProjectionDelegate @delegate, IEnumerable<string> handledEvents)
        {
            HandledEvents = handledEvents;
            this.@delegate = @delegate;
        }

        /// <summary>Initialize a new <see cref="DelegateProjection"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateProjection(ProjectionDelegate @delegate, params string[] handledEvents) : this(@delegate, (IEnumerable<string>)handledEvents) { }

        /// <summary>Initialize a new <see cref="DelegateProjection"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateProjection(ProjectionDelegate @delegate, IEnumerable<string> handledEvents)
        {
            HandledEvents = handledEvents;
            this.@delegate = @delegate;
        }

        public Task InvokeAsync(Event @event) => @delegate.DynamicInvoke(@event) as Task ?? Task.CompletedTask;
    }
}
