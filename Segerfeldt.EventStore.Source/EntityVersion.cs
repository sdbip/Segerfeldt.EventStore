using System;

namespace Segerfeldt.EventStore.Source
{
    public sealed class EntityVersion
    {
        public static EntityVersion New => new(-1);

        internal int Value { get; }

        private EntityVersion(int value)
        {
            Value = value;
        }

        public static EntityVersion Of(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Must be positive");
            return new EntityVersion(value);
        }
    }
}
