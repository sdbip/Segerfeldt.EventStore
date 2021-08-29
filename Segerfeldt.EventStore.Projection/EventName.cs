using System.Collections.Generic;

namespace Segerfeldt.EventStore.Projection
{
    public class EventName : ValueObject<EventName>
    {
        public string Name { get; }
        public string? EntityType { get; }

        public EventName(string entityType, string name)
        {
            EntityType = entityType;
            Name = name;
        }

        private EventName(string name)
        {
            Name = name;
        }

        public static implicit operator EventName(string name) => MatchingAnyEntity(name);
        private static EventName MatchingAnyEntity(string name) => new(name);

        public bool Handles(EventName @event) => IsSameName(@event) && IsMatchingType(@event);

        private bool IsSameName(EventName @event) => Name == @event.Name;
        private bool IsMatchingType(EventName @event) => EntityType is null || EntityType == @event.EntityType;

        protected override IEnumerable<object> GetEqualityComponents() => throw new System.NotImplementedException();
    }
}
