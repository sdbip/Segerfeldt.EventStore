namespace Segerfeldt.EventStore.Projection
{
    public interface IPollingStrategy
    {
        int NextDelay(int count);
    }
}
