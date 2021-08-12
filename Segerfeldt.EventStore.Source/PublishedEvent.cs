namespace Segerfeldt.EventStore.Source
{
    public class PublishedEvent
    {
        public string Name { get; }
        public string Details { get; }
        public string Actor { get; }

        public PublishedEvent(string name, string details, string actor)
        {
            Name = name;
            Details = details;
            Actor = actor;
        }
    }
}
