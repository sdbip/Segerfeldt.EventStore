namespace Segerfeldt.EventStore.Source
{
    public class PublishedEvent
    {
        public string Name { get; }
        public string Details { get; }

        public PublishedEvent(string name, string details)
        {
            Name = name;
            Details = details;
        }
    }
}
