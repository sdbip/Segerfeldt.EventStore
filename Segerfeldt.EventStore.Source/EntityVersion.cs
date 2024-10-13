using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

/// <summary>The version of an entity. Used for optimistic concurrency.</summary>
public sealed class EntityVersion : ValueObject<EntityVersion>
{
    /// <summary>A new entity that has not been published/stored yet</summary>
    public static EntityVersion New => new(-1);

    public static readonly EntityVersion Zero = new(0);

    /// <summary>The actual value of the version</summary>
    public int Value { get; }
    /// <summary>Whether this is a new entity, or it has been stored already</summary>
    public bool IsNew => Value < 0;

    private EntityVersion(int value) => Value = value;

    internal static EntityVersion Safe(int value) => new(value);

    /// <summary>Initialize a new <see cref="EntityVersion"/></summary>
    /// <param name="value">the actual value of the version</param>
    /// <returns>a valid <see cref="EntityVersion"/>with the specified <paramref name="value"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">if the value is negative</exception>
    public static Result<EntityVersion> Of(int value)
    {
        if (value < 0) return Failure.Error(new ArgumentOutOfRangeException(nameof(value), "Must be positive"));
        return new EntityVersion(value);
    }

    protected override IEnumerable<object> GetEqualityComponents() => [Value];

    /// <summary>The next <see cref="EntityVersion"/> after this</summary>
    /// <returns>a new <see cref="EntityVersion"/> with either the value 0 (if this is <see cref="New"/>), or this value + 1</returns>
    internal EntityVersion Next() => new(Value + 1);

    public override string ToString() => Value < 0 ? "[New]" : $"[{Value}]";
}
