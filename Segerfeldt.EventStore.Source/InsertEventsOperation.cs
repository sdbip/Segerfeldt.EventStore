using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace Segerfeldt.EventStore.Source
{
    internal sealed class InsertEventsOperation
    {
        private readonly IDbConnection connection;
        private readonly EntityId entityId;
        private readonly string actor;
        private readonly IEnumerable<UnpublishedEvent> events;
        private static readonly JsonSerializerOptions CamelCase = new() {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
        private EntityVersion? expectedVersion;

        public InsertEventsOperation(IConnectionFactory connectionFactory, EntityId entityId, string actor,
            params UnpublishedEvent[] events)
        {
            connection = connectionFactory.CreateConnection();
            this.entityId = entityId;
            this.actor = actor;
            this.events = events;
        }

        public InsertEventsOperation(IConnectionFactory connectionFactory, EntityId entityId, string actor, IEnumerable<UnpublishedEvent> events)
        {
            connection = connectionFactory.CreateConnection();
            this.entityId = entityId;
            this.actor = actor;
            this.events = events;
        }

        public void Run()
        {
            connection.Open();
            var transaction = connection.BeginTransaction();

            try
            {
                var currentVersion = GetCurrentVersion(entityId);
                if (expectedVersion is not null && currentVersion != expectedVersion) throw new ConcurrentUpdateException(expectedVersion, currentVersion);

                var position = GetCurrentPosition() + 1;
                var eventsAndVersions = events.Zip(InfiniteVersionsFrom(currentVersion.Next())).ToList();
                foreach (var (@event, version) in eventsAndVersions)
                    InsertEvent(@event, version, position);

                var lastInsertedVersion = eventsAndVersions.Last().Second;
                if (currentVersion.IsNew)
                    InsertEntity(entityId, lastInsertedVersion);
                else
                    UpdateVersion(entityId, lastInsertedVersion);
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

        private EntityVersion GetCurrentVersion(EntityId entityId)
        {
            var command = connection.CreateCommand("SELECT version FROM Entities WHERE id = @entityId",
                ("@entityId", entityId.ToString()));
            return command.ExecuteScalar() is int versionValue
                ? EntityVersion.Of(versionValue)
                : EntityVersion.New;
        }

        private static IEnumerable<EntityVersion> InfiniteVersionsFrom(EntityVersion next)
        {
            while (true)
            {
                yield return next;
                next = next.Next();
            }
        }

        private long GetCurrentPosition()
        {
            var command = connection.CreateCommand("SELECT MAX(position) FROM Events");
            return command.ExecuteScalar() as long? ?? -1;
        }

        private void InsertEvent(UnpublishedEvent @event, EntityVersion version, long position)
        {
            var command = connection.CreateCommand(
                "INSERT INTO Events (entity, name, details, actor, version, position)" +
                " VALUES (@entityId, @eventName, @details, @actor, @version, @position)",
                ("@entityId", entityId.ToString()),
                ("@eventName", @event.Name),
                ("@details", JsonSerializer.Serialize(@event.Details, CamelCase)),
                ("@actor", actor),
                ("@version", version.Value),
                ("@position", position));
            command.ExecuteNonQuery();
        }

        private void InsertEntity(EntityId id, EntityVersion version)
        {
            var command = connection.CreateCommand(
                "INSERT INTO Entities (id, version) VALUES (@id, @version)",
                ("@id", id.ToString()),
                ("@version", version.Value));
            command.ExecuteNonQuery();
        }

        private void UpdateVersion(EntityId id, EntityVersion version)
        {
            var command = connection.CreateCommand(
                "UPDATE Entities SET version = @version WHERE id = @id",
                ("(@id,", id.ToString()),
                ("@version", version.Value));
            command.ExecuteNonQuery();
        }

        public void ExpectVersion(EntityVersion version)
        {
            expectedVersion = version;
        }
    }
}
