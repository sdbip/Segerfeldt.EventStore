namespace Segerfeldt.EventStore.Projection
{
    public class EventName
    {
        public string Name { get; }
        public string? EntityType { get; }

        public EventName(string entityType, string name)
        {
            EntityType = entityType;
            Name = name;
        }

        public static implicit operator EventName(string name) => AcceptingAnyEntityType(name);
        private static EventName AcceptingAnyEntityType(string name) => new(null!, name);

        public bool IndicatesAcceptanceOf(EventName @event) => IsSameName(@event) && IsMatchingType(@event);

        private bool IsSameName(EventName @event) => Name == @event.Name;
        private bool IsMatchingType(EventName @event) => EntityType is null || EntityType == @event.EntityType;
    }
}
