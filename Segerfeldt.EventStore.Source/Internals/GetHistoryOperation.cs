using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.Internals
{
    internal class GetHistoryOperation
    {
        private readonly EntityId entityId;
        private readonly EntityVersion entityVersion;

        public GetHistoryOperation(EntityId entityId, EntityVersion entityVersion)
        {
            this.entityId = entityId;
            this.entityVersion = entityVersion;
        }

        public async Task<EntityHistory?> ExecuteAsync(DbConnection connection)
        {
            var command = connection.CreateCommand(
                "SELECT version FROM Entities WHERE id = @entityId;" +
                "SELECT * FROM Events WHERE entity = @entityId AND version > @entityVersion ORDER BY version",
                ("@entityId", entityId.ToString()),
                ("@entityVersion", entityVersion.Value));

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();
            var version = ReadEntityVersion(reader);
            if (version is null)
            {
                await connection.CloseAsync();
                return null;
            }

            var events = reader.NextResult()
                ? ReadEvents(reader).ToImmutableList()
                : ImmutableList<PublishedEvent>.Empty;

            await connection.CloseAsync();
            return new EntityHistory(version, events);
        }

        private static EntityVersion? ReadEntityVersion(IDataReader reader) =>
            reader.Read() ? EntityVersion.Of((int)reader[0]) : null;

        private static IEnumerable<PublishedEvent> ReadEvents(IDataReader reader)
        {
            while (reader.Read())
                yield return new PublishedEvent(
                    (string)reader["name"],
                    (string)reader["details"],
                    (string)reader["actor"],
                    ReadDateTimeUTC(reader)
                );
        }

        private static DateTime ReadDateTimeUTC(IDataRecord reader)
        {
            var timestampWithoutKind = reader["timestamp"] as DateTime? ?? DateTime.MinValue;
            return new DateTime(timestampWithoutKind.Ticks, DateTimeKind.Utc);
        }
    }
}
