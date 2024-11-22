using Segerfeldt.EventStore.Source.CommandAPI;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals;

public class EntityStoreRepository(EventStoreConnectionFactory connectionFactory) : IEntityStoreRepository
{
    public async Task<string?> GetTypeAsync(EntityId entityId, CancellationToken cancellationToken)
    {
        // TODO await using
        var connection = connectionFactory.CreateConnection();
        using var command = connection.CreateCommand("SELECT type FROM Entities WHERE id = @entityId");
        command.AddParameter("@entityId", entityId.ToString());

        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    public async Task<HistoryDAO?> GetHistoryAsync(EntityId entityId, Ordinal? after = null, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        // await using
        var connection = connectionFactory.CreateConnection();
        using var command = CreateCommand(transaction, connection, """
            SELECT type, version FROM Entities WHERE id = @entityId;
            SELECT * FROM Events WHERE entity_id = @entityId AND ordinal > @after ORDER BY ordinal
            """);
        command.AddParameter("@entityId", entityId.ToString());
        command.AddParameter("@after", after?.Value ?? -1);

        if (transaction is null) await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var entity = ReadEntityData(reader);
        if (entity is null)
        {
            if (transaction is null) await connection.CloseAsync();
            return null;
        }

        var events = await reader.NextResultAsync(cancellationToken)
            ? ReadEvents(reader).ToImmutableList()
            : [];

        if (transaction is null) await connection.CloseAsync();

        return new HistoryDAO
        {
            Type = entity.Value.Type,
            Version = entity.Value.Version,
            Events = [.. events],
        };

        static DbCommand CreateCommand(DbTransaction? transaction, DbConnection connection, string commandText) =>
            transaction?.CreateCommand(commandText) ?? connection.CreateCommand(commandText);
    }

    private static EntityDAO? ReadEntityData(DbDataReader reader)
    {
        return !reader.Read() ? null : new EntityDAO
        {
            Type = reader.GetString(0),
            Version = reader.GetInt32(1),
        };
    }

    private static IEnumerable<PublishedEventDAO> ReadEvents(DbDataReader reader)
    {
        while (reader.Read())
        {
            yield return new PublishedEventDAO
            {
                Name = (string)reader["name"],
                Details = (string)reader["details"],
                Actor = (string)reader["actor"],
                Ordinal = Convert.ToInt32(reader["ordinal"]),
                Timestamp = Convert.ToDouble(reader["timestamp"])
            };
        }
    }


    private readonly struct EntityDAO
    {
        public required string Type { get; init; }
        public required int Version { get; init; }
    }
}
