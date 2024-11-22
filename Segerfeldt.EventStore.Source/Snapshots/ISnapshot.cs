namespace Segerfeldt.EventStore.Source.Snapshots;

public interface ISnapshot<in TEntity> where TEntity : class, IEntity
{
    /// <summary>the id of this entity</summary>
    EntityId Id { get; }
    EntityType EntityType { get; }
    /// <summary>the ordinal of the last event added to this entity when the snapshot was made</summary>
    Ordinal Ordinal { get; }

    /// <summary>Restores the state of an entity from this snapshot</summary>
    /// <param name="entity"></param>
    void Restore(TEntity entity);
}
