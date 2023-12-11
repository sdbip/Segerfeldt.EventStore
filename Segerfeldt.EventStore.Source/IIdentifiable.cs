namespace Segerfeldt.EventStore.Source;

/// <summary>An object with identity</summary>
public interface IIdentifiable
{
    /// <summary>A unique identifier for this entity</summary>
    EntityId Id { get; }
}

public static class Identifiables
{
    public static bool IsSameAs(this IIdentifiable self, IIdentifiable other) => AreSame(self, other);

    public static bool AreSame(IIdentifiable? left, IIdentifiable? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.GetType() == right.GetType() && left.Id == right.Id;
    }
}
