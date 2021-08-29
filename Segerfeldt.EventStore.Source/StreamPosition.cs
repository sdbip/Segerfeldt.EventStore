namespace Segerfeldt.EventStore.Source
{
    public class StreamPosition
    {
        public long StorePosition { get; }
        public EntityVersion EntityVersion { get; }

        public StreamPosition(long storePosition, EntityVersion entityVersion)
        {
            StorePosition = storePosition;
            EntityVersion = entityVersion;
        }
    }
}
