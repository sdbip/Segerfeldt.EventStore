using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection
{
    public delegate Task AsyncSynchronizerDelegate(Event @event);
    public delegate void SynchronizerDelegate(Event @event);

    /// <summary><see cref="IReceptacle"/> implementation that uses a delegate</summary>
    public sealed class DelegateReceptacle : IReceptacle
    {
        private readonly Delegate @delegate;

        public IEnumerable<string> AcceptedEvents { get; }

        /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateReceptacle(AsyncSynchronizerDelegate @delegate, params string[] handledEvents) : this(@delegate, (IEnumerable<string>)handledEvents) { }

        /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateReceptacle(AsyncSynchronizerDelegate @delegate, IEnumerable<string> handledEvents)
        {
            AcceptedEvents = handledEvents;
            this.@delegate = @delegate;
        }

        /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateReceptacle(SynchronizerDelegate @delegate, params string[] handledEvents) : this(@delegate, (IEnumerable<string>)handledEvents) { }

        /// <summary>Initialize a new <see cref="DelegateReceptacle"/></summary>
        /// <param name="delegate">the delegate to call when events are notified</param>
        /// <param name="handledEvents">the events this delegate handles</param>
        public DelegateReceptacle(SynchronizerDelegate @delegate, IEnumerable<string> handledEvents)
        {
            AcceptedEvents = handledEvents;
            this.@delegate = @delegate;
        }

        public Task ReceiveAsync(Event @event) => @delegate.DynamicInvoke(@event) as Task ?? Task.CompletedTask;
    }
}
