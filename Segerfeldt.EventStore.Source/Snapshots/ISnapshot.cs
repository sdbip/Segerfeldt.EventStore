namespace Segerfeldt.EventStore.Source.Snapshots
{
    public interface ISnapshot<in TEntity> where TEntity : class, IEntity
    {
        /// <summary>the id of this entity</summary>
        EntityId Id { get; }
        /// <summary>the version of this entity when the snapshot was made</summary>
        EntityVersion Version { get; }

        /// <summary>Restores the state of an entity from this snapshot</summary>
        /// <param name="entity"></param>
        void Restore(TEntity entity);

        void NotFound() => throw new UnknownEntityException(Id);
    }
}
