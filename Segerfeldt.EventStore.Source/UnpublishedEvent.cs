namespace Segerfeldt.EventStore.Source
{
    public sealed class UnpublishedEvent
    {
        public string Name { get; }
        public object Details { get; }

        public UnpublishedEvent(string name, object details)
        {
            Details = details;
            Name = name;
        }
    }
}
