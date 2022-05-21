using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals;

internal sealed class GetHistoryOperation
{
    private readonly EntityId entityId;
    private readonly EntityVersion entityVersion;

    public GetHistoryOperation(EntityId entityId, EntityVersion entityVersion)
    {
        this.entityId = entityId;
        this.entityVersion = entityVersion;
    }

    public async Task<EntityHistory?> ExecuteAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand(
            "SELECT type, version FROM Entities WHERE id = @entityId;" +
            "SELECT * FROM Events WHERE entityId = @entityId AND version > @entityVersion ORDER BY version",
            ("@entityId", entityId.ToString()),
            ("@entityVersion", entityVersion.Value));

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var entityData = ReadEntityData(reader);
        if (entityData is null)
        {
            await connection.CloseAsync();
            return null;
        }

        var events = await reader.NextResultAsync(cancellationToken)
            ? ReadEvents(reader).ToImmutableList()
            : ImmutableList<PublishedEvent>.Empty;

        await connection.CloseAsync();

        var (type, version) = entityData.Value;
        return new EntityHistory(type, version, events);
    }

    private static (EntityType, EntityVersion)? ReadEntityData(IDataReader reader) =>
        reader.Read() ? (new EntityType((string)reader[0]), EntityVersion.Of((int)reader[1])) : null;

    private static IEnumerable<PublishedEvent> ReadEvents(IDataReader reader)
    {
        while (reader.Read())
        {
            yield return new PublishedEvent(
                (string)reader["name"],
                (string)reader["details"],
                (string)reader["actor"],
                DateTimeOffset(reader["timestamp"])
            );
        }
    }

    private static DateTimeOffset DateTimeOffset(object timestamp) =>
        new(JulianDayConverter.FromJulianDay(Convert.ToDouble(timestamp)), TimeSpan.Zero);
}
