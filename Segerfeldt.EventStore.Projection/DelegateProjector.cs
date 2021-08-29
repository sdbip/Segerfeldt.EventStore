using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public delegate Task AsyncProjectionDelegate(Event @event);
    public delegate void ProjectionDelegate(Event @event);

    /// <summary>IProjection implementation that uses a delegate</summary>
    public class DelegateProjector : IProjector
    {
        private readonly Delegate @delegate;

        public IEnumerable<EventName> HandledEvents { get; }

        /// <summary>Initialize a new <see cref="DelegateProjector"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateProjector(AsyncProjectionDelegate @delegate, params EventName[] handledEvents) : this(@delegate, (IEnumerable<EventName>)handledEvents) { }

        /// <summary>Initialize a new <see cref="DelegateProjector"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateProjector(AsyncProjectionDelegate @delegate, IEnumerable<EventName> handledEvents)
        {
            HandledEvents = handledEvents;
            this.@delegate = @delegate;
        }

        /// <summary>Initialize a new <see cref="DelegateProjector"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateProjector(ProjectionDelegate @delegate, params EventName[] handledEvents) : this(@delegate, (IEnumerable<EventName>)handledEvents) { }

        /// <summary>Initialize a new <see cref="DelegateProjector"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateProjector(ProjectionDelegate @delegate, IEnumerable<EventName> handledEvents)
        {
            HandledEvents = handledEvents;
            this.@delegate = @delegate;
        }

        public Task InvokeAsync(Event @event) => @delegate.DynamicInvoke(@event) as Task ?? Task.CompletedTask;
    }
}
