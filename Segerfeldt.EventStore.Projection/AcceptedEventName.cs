namespace Segerfeldt.EventStore.Projection
{
    /// <summary>A </summary>
    public readonly struct AcceptedEventName
    {
        /// <summary>The name acceptable events</summary>
        public string Name { get; }
        /// <summary>
        /// An optional entity-type used for namespacing allowing unacceptable events to have the same name
        /// </summary>
        public string? EntityType { get; }

        public AcceptedEventName(string entityType, string name)
        {
            EntityType = entityType;
            Name = name;
        }

        public static implicit operator AcceptedEventName(string name) => AcceptingAnyEntityType(name);
        private static AcceptedEventName AcceptingAnyEntityType(string name) => new(null!, name);

        /// <summary>
        /// Whether this <see cref="AcceptedEventName"/> is a match for a given name and type.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public bool IndicatesAcceptanceOf(string eventName, string entityType) =>
            Name == eventName && MatchesEntityType(entityType);

        private bool MatchesEntityType(string type) => EntityType is null || EntityType == type;
    }
}
