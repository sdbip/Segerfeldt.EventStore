using System;

namespace Segerfeldt.EventStore.Source
{
    public sealed class EntityVersion
    {
        public static EntityVersion New => new(-1);

        internal int Value { get; }
        public bool IsNew => Value < 0;

        private EntityVersion(int value) => Value = value;

        public static EntityVersion Of(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Must be positive");
            return new EntityVersion(value);
        }

        public EntityVersion Next() => Of(Value < 0 ? 0 : Value + 1);

        public static bool operator ==(EntityVersion left, EntityVersion right) => Equals(left, right);
        public static bool operator !=(EntityVersion left, EntityVersion right) => !Equals(left, right);

        public override bool Equals(object? obj) => obj is EntityVersion other && other.Value == Value;
        public override int GetHashCode() => Value;
        public override string ToString() => Value == -2 ? "[Missing]" : Value == -1 ? "[New]" : $"[{Value}]";
    }
}
