using System.Collections.Generic;

namespace Segerfeldt.EventStore.Source;

public sealed class TypedEntityId(EntityId id, EntityType entityType) : ValueObject<TypedEntityId>
{
    public EntityId Value { get; } = id;
    public EntityType Type { get; } = entityType;

    public TypedEntityId(string id, string entityType) : this(EntityId.Value(id), EntityType.Name(entityType)) { }

    protected override IEnumerable<object> GetEqualityComponents() => [Value, Type];
}
