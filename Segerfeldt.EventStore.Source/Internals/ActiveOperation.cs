using Segerfeldt.EventStore.Utils;

using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals
{
    internal abstract class ActiveOperation
    {
        private readonly DbTransaction transaction;
        private readonly string actor;

        public ActiveOperation(DbTransaction transaction, string actor)
        {
            this.transaction = transaction;
            this.actor = actor;
        }

        protected async Task<EntityVersion> GetCurrentVersionAsync(EntityId entityId)
        {
            var command = transaction.CreateCommand("SELECT version FROM Entities WHERE id = @entityId",
                ("@entityId", entityId.ToString()));
            return await command.ExecuteScalarAsync() is int versionValue
                ? EntityVersion.Of(versionValue)
                : EntityVersion.New;
        }

        protected async Task<long> GetCurrentPositionAsync()
        {
            var command = transaction.CreateCommand("SELECT MAX(position) FROM Events");
            return await command.ExecuteScalarAsync() as long? ?? -1;
        }

        protected async Task InsertEventAsync(EntityId entityId, UnpublishedEvent @event, EntityVersion version, long position)
        {
            var command = transaction.CreateCommand(
                "INSERT INTO Events (entity, name, details, actor, version, position)" +
                " VALUES (@entityId, @eventName, @details, @actor, @version, @position)",
                ("@entityId", entityId.ToString()),
                ("@eventName", @event.Name),
                ("@details", JSON.Serialize(@event.Details)),
                ("@actor", actor),
                ("@version", version.Value),
                ("@position", position));
            await command.ExecuteNonQueryAsync();
        }

        protected async Task InsertEntityAsync(EntityId id, EntityVersion version)
        {
            var command = transaction.CreateCommand(
                "INSERT INTO Entities (id, version) VALUES (@id, @version)",
                ("@id", id.ToString()),
                ("@version", version.Value));
            await command.ExecuteNonQueryAsync();
        }

        protected async Task UpdateVersionAsync(EntityId id, EntityVersion version)
        {
            var command = transaction.CreateCommand(
                "UPDATE Entities SET version = @version WHERE id = @id",
                ("@id", id.ToString()),
                ("@version", version.Value));
            await command.ExecuteNonQueryAsync();
        }

        protected async Task InsertEventsAsync(EntityId entityId, IEnumerable<(UnpublishedEvent @event, EntityVersion version)> eventsAndVersions)
        {
            var ev = eventsAndVersions.ToList();
            var position = await GetCurrentPositionAsync() + 1;
            foreach (var (@event, version) in ev)
                await InsertEventAsync(entityId, @event, version, position);

            var lastInsertedVersion = ev.Last().version;
            await UpdateVersionAsync(entityId, lastInsertedVersion);
        }
    }
}
