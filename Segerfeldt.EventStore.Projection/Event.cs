namespace Segerfeldt.EventStore.Projection
{
    public class Event
    {
        public string Name { get; }

        public Event(string name)
        {
            Name = name;
        }
    }
}
