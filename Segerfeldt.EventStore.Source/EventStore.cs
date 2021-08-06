using System.Text.Json;

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
            using var operation = AtomicOperation.BeginTransaction(connectionFactory);

            var (version, position) = operation.GetVersionAndPosition(entityId.ToString());

            if (version is null)
                operation.InsertEntity(entityId.ToString(), 1);
            else
                operation.UpdateEntityVersion(entityId.ToString(), version.Value);

            var details = JsonSerializer.Serialize(@event.Details,
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            operation.InsertEvent(entityId.ToString(), @event.Name, details, actor, version + 1 ?? 1, position + 1 ?? 1);

            operation.Commit();
        }

        public void PublishChanges(IEntity entity, string actor)
        {
            using var operation = AtomicOperation.BeginTransaction(connectionFactory);

            var (previousVersion, previousPosition) = operation.GetVersionAndPosition(entity.Id.ToString());
            if ((previousVersion ?? -1) != entity.Version.Value) throw new ConcurrentUpdateException(entity.Version, previousVersion);

            var version = previousVersion ?? 0;
            var position = previousPosition + 1 ?? 1;

            foreach (var @event in entity.UnpublishedEvents)
            {
                version++;
                var details = JsonSerializer.Serialize(@event.Details,
                    new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});

                operation.InsertEvent(entity.Id.ToString(), @event.Name, details, actor, version, position);
            }

            operation.Commit();
        }
    }
}
