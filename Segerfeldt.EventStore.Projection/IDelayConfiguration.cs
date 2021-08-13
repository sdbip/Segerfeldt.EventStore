namespace Segerfeldt.EventStore.Projection
{
    public interface IDelayConfiguration
    {
        int NextDelay(int count);
    }
}
