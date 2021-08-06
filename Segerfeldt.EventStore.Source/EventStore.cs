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

            var currentVersion = operation.GetVersion(entityId);
            var nextVersion = currentVersion.Next();

            var details = JsonSerializer.Serialize(@event.Details,
                new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            operation.InsertEvent(entityId, @event.Name, details, actor, nextVersion, operation.GetPosition() + 1 ?? 0);

            if (currentVersion.IsNew)
                operation.InsertEntity(entityId, nextVersion);
            else
                operation.UpdateEntityVersion(entityId, nextVersion);

            operation.Commit();
        }

        public void PublishChanges(IEntity entity, string actor)
        {
            using var operation = AtomicOperation.BeginTransaction(connectionFactory);

            var currentVersion = operation.GetVersion(entity.Id);
            if (currentVersion != entity.Version) throw new ConcurrentUpdateException(entity.Version, currentVersion);

            var position = operation.GetPosition() + 1 ?? 0;
            var nextVersion = currentVersion;
            foreach (var @event in entity.UnpublishedEvents)
            {
                nextVersion++;
                var details = JsonSerializer.Serialize(@event.Details,
                    new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});

                operation.InsertEvent(entity.Id, @event.Name, details, actor, nextVersion, position);
            }

            if (currentVersion.IsNew)
                operation.InsertEntity(entity.Id, nextVersion);
            else
                operation.UpdateEntityVersion(entity.Id, nextVersion);

            operation.Commit();
        }
    }
}
