using System;
using System.Data;
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

        public void Publish(EntityId entityId, UnpublishedEvent @event, string actor) {
            PerformAtomically(connection =>
            {
                var (version, position) = connection.GetVersionAndPosition(entityId.ToString());

                if (version is null)
                    connection.InsertEntity(entityId.ToString(), 1);
                else
                    connection.UpdateEntityVersion(entityId.ToString(), version.Value);

                var details = JsonSerializer.Serialize(@event.Details,
                    new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
                connection.InsertEvent(entityId.ToString(), @event.Name, details, actor, version + 1 ?? 1, position + 1 ?? 1);
            });
        }

        public void PublishChanges(IEntity entity, string actor)
        {
            PerformAtomically(connection =>
            {
                var (previousVersion, previousPosition) = connection.GetVersionAndPosition(entity.Id.ToString());
                if ((previousVersion ?? -1) != entity.Version.Value) throw new ConcurrentUpdateException(entity.Version, previousVersion);

                var version = previousVersion ?? 0;
                var position = previousPosition + 1 ?? 1;

                foreach (var @event in entity.UnpublishedEvents)
                {
                    version++;
                    var details = JsonSerializer.Serialize(@event.Details,
                        new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});

                    connection.InsertEvent(entity.Id.ToString(), @event.Name, details, actor, version, position);
                }
            });
        }

        private void PerformAtomically(Action<IDbConnection> action)
        {
            var connection = connectionFactory.CreateConnection();
            connection.Open();
            var transaction = connection.BeginTransaction();

            try
            {
                action(connection);
            }
            catch
            {
                transaction.Rollback();
                connection.Close();
                throw;
            }

            transaction.Commit();
            connection.Close();
        }
    }
}
