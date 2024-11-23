using System;
using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

/// <summary>The version of an entity. Used for optimistic concurrency.</summary>
public sealed class EntityVersion : ValueObject<EntityVersion>
{
    /// <summary>A new entity that has not been published/stored yet</summary>
    public static EntityVersion New => new(null!);

    public static readonly EntityVersion Zero = new(Ordinal.Zero);

    /// <summary>The ordinal value of the version</summary>
    public Ordinal? Ordinal { get; }

    public EntityVersion(Ordinal value) => Ordinal = value;

    /// <summary>Initialize a new <see cref="EntityVersion"/></summary>
    /// <param name="value">the actual value of the version</param>
    /// <returns>a valid <see cref="EntityVersion"/>with the specified <paramref name="value"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">if the value is negative</exception>
    public static EntityVersion Of(int value) => new(Ordinal.Of(value));

    /// <inheritdoc />
    protected override IEnumerable<object> GetEqualityComponents() => Ordinal is null ? [] : [Ordinal];

    /// <summary>The next <see cref="EntityVersion"/> after this</summary>
    /// <returns>a new <see cref="EntityVersion"/> with either the value 0 (if this is <see cref="New"/>), or this value + 1</returns>
    internal EntityVersion Next() => new(NextOrdinal());
    internal Ordinal NextOrdinal() => Ordinal?.Next() ?? Ordinal.Zero;

    /// <inheritdoc />
    public override string ToString() => Ordinal is null ? "[New]" : $"[{Ordinal.Value}]";
}
