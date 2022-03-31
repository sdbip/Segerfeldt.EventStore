using System;
using System.Collections.Generic;
using System.Linq;

namespace Segerfeldt.EventStore.Source;

/// <summary>Abstract superclass that can help creating value-object classes.</summary>
/// <typeparam name="TSubclass"></typeparam>
public abstract class ValueObject<TSubclass> where TSubclass : ValueObject<TSubclass>
{
    /// <summary>The definition of how this value object is compared with others of the same type</summary>
    /// <returns>the ordered values of all the components needed to compare this specific value with other instance</returns>
    protected abstract IEnumerable<object> GetEqualityComponents();

    public static bool operator ==(ValueObject<TSubclass>? left, ValueObject<TSubclass>? right) => Equals(left, right);
    public static bool operator !=(ValueObject<TSubclass>? left, ValueObject<TSubclass>? right) => !Equals(left, right);

    public override bool Equals(object? obj) =>
        obj is TSubclass other && other.GetType() == GetType() &&
        other.GetEqualityComponents().SequenceEqual(GetEqualityComponents());

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var element in GetEqualityComponents())
            hash.Add(element);
        return hash.ToHashCode();
    }
}
