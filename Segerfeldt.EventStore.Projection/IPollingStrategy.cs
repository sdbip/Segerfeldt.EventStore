namespace Segerfeldt.EventStore.Projection;

/// <summary>A strategy for how often to poll for new changes</summary>
public interface IPollingStrategy
{
    /// <summary>Method returning the delay from one poll to the next</summary>
    /// <param name="count">thw number of events handled in this batch</param>
    /// <returns>number of milliseconds to wait until next poll</returns>
    int NextDelay(int count);
}
