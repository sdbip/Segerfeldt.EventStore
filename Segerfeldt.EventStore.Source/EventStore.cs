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
            var command = new InsertEventsOperation(entityId, actor, @event);
            command.Execute(connection);
        }

        public void PublishChanges(IEntity entity, string actor)
        {
            var command = new InsertEventsOperation(entity.Id, actor, entity.UnpublishedEvents) { ExpectedVersion = entity.Version };
            command.Execute(connection);
        }
    }
}
