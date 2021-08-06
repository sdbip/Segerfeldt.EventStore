using System.Data;

namespace Segerfeldt.EventStore.Source
{
    public sealed class EventStore
    {
        private readonly IDbConnection connection;

        public EventStore(IDbConnection connection)
        {
            this.connection = connection;
        }

        public void Publish(EntityId entityId, UnpublishedEvent @event, string actor)
        {
            var operation = new InsertEventsOperation(connection, entityId, actor, @event);
            operation.Run();
        }

        public void PublishChanges(IEntity entity, string actor)
        {
            var operation = new InsertEventsOperation(connection, entity.Id, actor, entity.UnpublishedEvents);
            operation.ExpectVersion(entity.Version);
            operation.Run();
        }
    }
}
