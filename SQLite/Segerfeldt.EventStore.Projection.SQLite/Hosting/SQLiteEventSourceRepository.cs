using Segerfeldt.EventStore.Shared;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Segerfeldt.EventStore.Projection.SQLite.Hosting;

public class SQLiteEventSourceRepository(IDbConnection connection) : IEventSourceRepository
{
    private readonly IDbConnection connection = connection;

    public IEnumerable<Event> GetEvents(long afterPosition, int maxCount)
    {
        using var command = connection.CreateCommand("""
            SELECT Events.*, Entities.type AS entity_type FROM Events
                JOIN Entities ON Entities.id = Events.entity_id
                WHERE position > @position
            LIMIT @maxCount
            """);
        command.AddParameter("@position", afterPosition);
        command.AddParameter("@maxCount", maxCount);

        connection.Open();
        try { return command.ExecuteReader().AllRowsAs(ReadEvent); }
        catch (DbException) { return []; } // No connection => no events.
        finally { connection.Close(); }
    }

    private static Event ReadEvent(IDataRecord record) => new(
        record.GetString(record.GetOrdinal("entity_id")),
        record.GetString(record.GetOrdinal("entity_type")),
        record.GetString(record.GetOrdinal("name")),
        record.GetString(record.GetOrdinal("details")),
        Convert.ToInt16(record.GetValue(record.GetOrdinal("ordinal"))),
        record.GetInt64(record.GetOrdinal("position")));
}
