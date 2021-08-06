namespace Segerfeldt.EventStore.Source
{
    public sealed class EventStore
    {
        private readonly IConnectionFactory connectionFactory;

        public EventStore(IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public void Publish(EntityId entityId, UnpublishedEvent @event, string actor)
        {
            var operation = new InsertEventsOperation(connectionFactory, entityId, actor, @event);
            operation.Run();
        }

        public void PublishChanges(IEntity entity, string actor)
        {
            var operation = new InsertEventsOperation(connectionFactory, entity.Id, actor, entity.UnpublishedEvents);
            operation.ExpectVersion(entity.Version);
            operation.Run();
        }
    }
}
