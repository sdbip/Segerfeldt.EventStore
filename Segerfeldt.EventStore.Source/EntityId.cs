namespace Segerfeldt.EventStore.Source
{
    public sealed class EntityId
    {
        private readonly string value;

        public EntityId(string value)
        {
            this.value = value;
        }

        public override bool Equals(object? obj) => obj is EntityId other && other.value == value;
        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value;
    }
}
