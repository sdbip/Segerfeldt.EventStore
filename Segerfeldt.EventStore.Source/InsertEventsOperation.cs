using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace Segerfeldt.EventStore.Source
{
    internal sealed class InsertEventsOperation
    {
        private readonly EntityId entityId;
        private readonly string actor;
        private readonly IEnumerable<UnpublishedEvent> events;
        public EntityVersion? ExpectedVersion { get; init; }

        public InsertEventsOperation(EntityId entityId, string actor, params UnpublishedEvent[] events)
        {
            this.entityId = entityId;
            this.actor = actor;
            this.events = events;
        }

        public InsertEventsOperation(EntityId entityId, string actor, IEnumerable<UnpublishedEvent> events)
        {
            this.entityId = entityId;
            this.actor = actor;
            this.events = events.ToList();
        }

        public void Execute(IDbConnection connection)
        {
            ActiveOperation.Run(connection, this);
        }

        private sealed class ActiveOperation
        {
            private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            private readonly IDbTransaction transaction;
            private readonly InsertEventsOperation operation;

            private ActiveOperation(IDbTransaction transaction, InsertEventsOperation operation)
            {
                this.transaction = transaction;
                this.operation = operation;
            }

            public static void Run(IDbConnection connection, InsertEventsOperation operation)
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                var activeOperation = new ActiveOperation(transaction, operation);

                try
                {
                    activeOperation.Run();
                    transaction.Commit();
                    connection.Close();
                }
                catch
                {
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }
            }

            private void Run()
            {
                var currentVersion = GetCurrentVersion();
                if (operation.ExpectedVersion is not null && currentVersion != operation.ExpectedVersion)
                    throw new ConcurrentUpdateException(operation.ExpectedVersion, currentVersion);

                if (currentVersion.IsNew) InsertEntity(operation.entityId, EntityVersion.Of(1));

                var position = GetCurrentPosition() + 1;
                var eventsAndVersions = operation.events.Zip(InfiniteVersionsFrom(currentVersion.Next())).ToList();
                foreach (var (@event, version) in eventsAndVersions)
                    InsertEvent(@event, version, position);

                var lastInsertedVersion = eventsAndVersions.Last().Second;
                UpdateVersion(operation.entityId, lastInsertedVersion);
            }

            private static IEnumerable<EntityVersion> InfiniteVersionsFrom(EntityVersion first)
            {
                var next = first;
                while (true)
                {
                    yield return next;
                    next = next.Next();
                }
                // ReSharper disable once IteratorNeverReturns
            }

            private EntityVersion GetCurrentVersion()
            {
                var command = transaction.CreateCommand("SELECT version FROM Entities WHERE id = @entityId",
                    ("@entityId", operation.entityId.ToString()));
                return command.ExecuteScalar() is int versionValue
                    ? EntityVersion.Of(versionValue)
                    : EntityVersion.New;
            }

            private long GetCurrentPosition()
            {
                var command = transaction.CreateCommand("SELECT MAX(position) FROM Events");
                return command.ExecuteScalar() as long? ?? -1;
            }

            private void InsertEvent(UnpublishedEvent @event, EntityVersion version, long position)
            {
                var command = transaction.CreateCommand(
                    "INSERT INTO Events (entity, name, details, actor, version, position)" +
                    " VALUES (@entityId, @eventName, @details, @actor, @version, @position)",
                    ("@entityId", operation.entityId.ToString()),
                    ("@eventName", @event.Name),
                    ("@details", JsonSerializer.Serialize(@event.Details, CamelCase)),
                    ("@actor", operation.actor),
                    ("@version", version.Value),
                    ("@position", position));
                command.ExecuteNonQuery();
            }

            private void InsertEntity(EntityId id, EntityVersion version)
            {
                var command = transaction.CreateCommand(
                    "INSERT INTO Entities (id, version) VALUES (@id, @version)",
                    ("@id", id.ToString()),
                    ("@version", version.Value));
                command.ExecuteNonQuery();
            }

            private void UpdateVersion(EntityId id, EntityVersion version)
            {
                var command = transaction.CreateCommand(
                    "UPDATE Entities SET version = @version WHERE id = @id",
                    ("@id", id.ToString()),
                    ("@version", version.Value));
                command.ExecuteNonQuery();
            }
        }
    }
}
