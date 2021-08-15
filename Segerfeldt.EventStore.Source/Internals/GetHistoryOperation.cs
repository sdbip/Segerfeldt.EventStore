using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;

namespace Segerfeldt.EventStore.Source.Internals
{
    internal class GetHistoryOperation
    {
        private readonly EntityId entityId;

        public GetHistoryOperation(EntityId entityId)
        {
            this.entityId = entityId;
        }

        public EntityHistory? Execute(IDbConnection connection)
        {
            var command = connection.CreateCommand(
                "SELECT version FROM Entities WHERE id = @entityId;" +
                "SELECT * FROM Events WHERE entity = @entityId ORDER BY version",
                ("@entityId", entityId.ToString()));

            connection.Open();
            using var reader = command.ExecuteReader();
            var version = ReadEntityVersion(reader);
            if (version is null)
            {
                connection.Close();
                return null;
            }

            var events = reader.NextResult()
                ? ReadEvents(reader).ToImmutableList()
                : ImmutableList<PublishedEvent>.Empty;

            connection.Close();
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
