using JetBrains.Annotations;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Segerfeldt.EventStore.Source
{
    public abstract class EntityBase : IEntity
    {
        private readonly List<UnpublishedEvent> unpublishedEvents = new();
        public EntityId Id { get; }
        public EntityVersion Version { get; }
        public IEnumerable<UnpublishedEvent> UnpublishedEvents => unpublishedEvents.ToImmutableList();

        protected EntityBase(EntityId id, EntityVersion version)
        {
            Id = id;
            Version = version;
        }

        protected void Add(UnpublishedEvent @event)
        {
            unpublishedEvents.Add(@event);
        }

        public void ReplayEvents(IEnumerable<PublishedEvent> events)
        {
            foreach (var @event in events) ReplayEvent(@event);
        }

        private void ReplayEvent(PublishedEvent @event)
        {
            var methods = FindReplayMethods(@event);
            foreach (var method in methods)
                InvokeReplayMethod(method, @event);
        }

        private IEnumerable<MethodInfo> FindReplayMethods(PublishedEvent @event) =>
            GetPublicInstanceMethods().Where(ReplaysEvent(@event.Name));

        private IEnumerable<MethodInfo> GetPublicInstanceMethods() => GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance);

        private static Func<MethodInfo, bool> ReplaysEvent(string eventName) =>
            m => m.GetCustomAttribute<ReplaysEventAttribute>()?.Event == eventName;

        private void InvokeReplayMethod(MethodBase method, PublishedEvent @event)
        {
            var type = method.GetParameters()[0].ParameterType;
            var args = new[] { type == typeof(PublishedEvent) ? @event : @event.DetailsAs(type) };
            method.Invoke(this, args);
        }

        [AttributeUsage(AttributeTargets.Method)]
        [MeansImplicitUse]
        protected class ReplaysEventAttribute : Attribute
        {
            public string Event { get; }

            public ReplaysEventAttribute(string @event)
            {
                Event = @event;
            }
        }
    }
}
