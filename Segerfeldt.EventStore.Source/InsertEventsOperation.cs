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
            new ActiveOperation(connection, this).Run();
        }

        private sealed class ActiveOperation
        {
            private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            private readonly IDbConnection connection;
            private IDbTransaction? transaction;
            private readonly InsertEventsOperation operation;

            public ActiveOperation(IDbConnection connection, InsertEventsOperation operation)
            {
                this.connection = connection;
                this.operation = operation;
            }

            public void Run()
            {
                connection.Open();
                transaction = connection.BeginTransaction();

                try
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
                catch
                {
                    transaction.Rollback();
                    connection.Close();
                    throw;
                }

                transaction.Commit();
                connection.Close();
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
                var command = connection.CreateCommand("SELECT version FROM Entities WHERE id = @entityId",
                    ("@entityId", operation.entityId.ToString()));
                command.Transaction = transaction;
                return command.ExecuteScalar() is int versionValue
                    ? EntityVersion.Of(versionValue)
                    : EntityVersion.New;
            }

            private long GetCurrentPosition()
            {
                var command = connection.CreateCommand("SELECT MAX(position) FROM Events");
                command.Transaction = transaction;
                return command.ExecuteScalar() as long? ?? -1;
            }

            private void InsertEvent(UnpublishedEvent @event, EntityVersion version, long position)
            {
                var command = connection.CreateCommand(
                    "INSERT INTO Events (entity, name, details, actor, version, position)" +
                    " VALUES (@entityId, @eventName, @details, @actor, @version, @position)",
                    ("@entityId", operation.entityId.ToString()),
                    ("@eventName", @event.Name),
                    ("@details", JsonSerializer.Serialize(@event.Details, CamelCase)),
                    ("@actor", operation.actor),
                    ("@version", version.Value),
                    ("@position", position));
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }

            private void InsertEntity(EntityId id, EntityVersion version)
            {
                var command = connection.CreateCommand(
                    "INSERT INTO Entities (id, version) VALUES (@id, @version)",
                    ("@id", id.ToString()),
                    ("@version", version.Value));
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }

            private void UpdateVersion(EntityId id, EntityVersion version)
            {
                var command = connection.CreateCommand(
                    "UPDATE Entities SET version = @version WHERE id = @id",
                    ("@id", id.ToString()),
                    ("@version", version.Value));
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
        }
    }
}
