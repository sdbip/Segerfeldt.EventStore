namespace Segerfeldt.EventStore.Source
{
    /// <summary>And entity identifier</summary>
    public sealed class EntityId
    {
        private readonly string value;

        /// <summary>Initialize an identifier</summary>
        /// <param name="value">The string value that uniquely identifies the identity (and its events)</param>
        public EntityId(string value)
        {
            this.value = value;
        }

        public override bool Equals(object? obj) => obj is EntityId other && other.value == value;
        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value;
    }
}
