using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals
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

        public async Task ExecuteAsync(DbConnection connection)
        {
            await connection.OpenAsync();
            var transaction = await connection.BeginTransactionAsync();
            var activeOperation = new ActiveOperation(transaction, this);

            try
            {
                await activeOperation.RunAsync();
                await transaction.CommitAsync();
                await connection.CloseAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                await connection.CloseAsync();
                throw;
            }
        }

        private sealed class ActiveOperation
        {
            private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            private readonly DbTransaction transaction;
            private readonly InsertEventsOperation operation;

            public ActiveOperation(DbTransaction transaction, InsertEventsOperation operation)
            {
                this.transaction = transaction;
                this.operation = operation;
            }

            public async Task RunAsync()
            {
                var currentVersion = await GetCurrentVersionAsync();
                if (operation.ExpectedVersion is not null && currentVersion != operation.ExpectedVersion)
                    throw new ConcurrentUpdateException(operation.ExpectedVersion, currentVersion);

                if (currentVersion.IsNew) await InsertEntityAsync(operation.entityId, EntityVersion.Of(1));

                var position = await GetCurrentPositionAsync() + 1;
                var eventsAndVersions = operation.events.Zip(InfiniteVersionsFrom(currentVersion.Next())).ToList();
                foreach (var (@event, version) in eventsAndVersions)
                    await InsertEventAsync(@event, version, position);

                var lastInsertedVersion = eventsAndVersions.Last().Second;
                await UpdateVersionAsync(operation.entityId, lastInsertedVersion);
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

            private async Task<EntityVersion> GetCurrentVersionAsync()
            {
                var command = transaction.CreateCommand("SELECT version FROM Entities WHERE id = @entityId",
                    ("@entityId", operation.entityId.ToString()));
                return await command.ExecuteScalarAsync() is int versionValue
                    ? EntityVersion.Of(versionValue)
                    : EntityVersion.New;
            }

            private async Task<long> GetCurrentPositionAsync()
            {
                var command = transaction.CreateCommand("SELECT MAX(position) FROM Events");
                return await command.ExecuteScalarAsync() as long? ?? -1;
            }

            private async Task InsertEventAsync(UnpublishedEvent @event, EntityVersion version, long position)
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
                await command.ExecuteNonQueryAsync();
            }

            private async Task InsertEntityAsync(EntityId id, EntityVersion version)
            {
                var command = transaction.CreateCommand(
                    "INSERT INTO Entities (id, version) VALUES (@id, @version)",
                    ("@id", id.ToString()),
                    ("@version", version.Value));
                await command.ExecuteNonQueryAsync();
            }

            private async Task UpdateVersionAsync(EntityId id, EntityVersion version)
            {
                var command = transaction.CreateCommand(
                    "UPDATE Entities SET version = @version WHERE id = @id",
                    ("@id", id.ToString()),
                    ("@version", version.Value));
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
