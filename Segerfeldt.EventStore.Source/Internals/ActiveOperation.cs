using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Source.Internals;

internal abstract class ActiveOperation(DbTransaction transaction, string actor)
{
    private readonly DbTransaction transaction = transaction;
    private readonly string actor = actor;

    protected async Task<EntityVersion> GetCurrentVersionAsync(EntityId entityId)
    {
        using var command = transaction.CreateCommand("SELECT version FROM Entities WHERE id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());
        var scalar = await command.ExecuteScalarAsync();
        return scalar is null
            ? EntityVersion.New
            : EntityVersion.Of(Convert.ToInt32(scalar));
    }

    protected async Task InsertEventAsync(EntityId entityId, UnpublishedEvent @event, EntityVersion ordinal, long position)
    {
        using var command = transaction.CreateCommand(
            "INSERT INTO Events (entity_id, name, details, actor, ordinal, position)" +
            " VALUES (@entityId, @eventName, @details, @actor, @ordinal, @position)");
        command.AddParameter("@entityId", entityId.ToString());
        command.AddParameter("@eventName", @event.Name);
        command.AddParameter("@details", JSON.Serialize(@event.Details));
        command.AddParameter("@actor", actor);
        command.AddParameter("@ordinal", ordinal.Value);
        command.AddParameter("@position", position);
        await command.ExecuteNonQueryAsync();
    }

    protected async Task InsertEntityAsync(EntityId id, EntityType type, EntityVersion version)
    {
        using var command = transaction.CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@id, @type, @version)");
        command.AddParameter("@id", id.ToString());
        command.AddParameter("@type", type.ToString());
        command.AddParameter("@version", version.Value);
        await command.ExecuteNonQueryAsync();
    }

    protected async Task UpdateVersionAsync(EntityId id, EntityVersion version)
    {
        using var command = transaction.CreateCommand("UPDATE Entities SET version = @version WHERE id = @id");
        command.AddParameter("@id", id.ToString());
        command.AddParameter("@version", version.Value);
        await command.ExecuteNonQueryAsync();
    }

    internal async Task<UpdatedStorePosition> InsertEventsForEntities(IEnumerable<EntityData> entities)
    {
        var position = await GetNextPositionAsync();

        var entityVersions = await Task.WhenAll(
            entities.Select(async entity =>
            {
                var (id, _, currentVersion, events) = entity;
                var incrementingVersions = InfiniteVersionsFrom(currentVersion.Next());
                var tuples = events.Zip(incrementingVersions).ToList();
                foreach (var (@event, version) in tuples)
                    await InsertEventAsync(id, @event, version, position);

                var (_, lastInsertedVersion) = tuples.Last();
                await UpdateVersionAsync(id, lastInsertedVersion);
                return (id, lastInsertedVersion);
            })
        );

        return new UpdatedStorePosition(position, entityVersions);

        // ReSharper disable once IteratorNeverReturns
        static IEnumerable<EntityVersion> InfiniteVersionsFrom(EntityVersion first)
        {
            var next = first;
            while (true)
            {
                yield return next;
                next = next.Next();
            }
        }
    }

    private async Task<long> GetNextPositionAsync()
    {
        var command = transaction.CreateCommand("SELECT max(position) + 1 FROM Events");
        var result = await command.ExecuteScalarAsync();
        return result as long? ?? 0;
    }

    internal sealed record EntityData(EntityId Id, EntityType Type, EntityVersion Version, IEnumerable<UnpublishedEvent> Events);
}
