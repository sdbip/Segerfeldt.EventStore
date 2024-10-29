using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Projection.MSSQL.Hosting;

/// <summary>The default <see cref="IEventSourceRepository"/> implementation</summary>
/// <param name="connection">A connection to the source write-model</param>
public sealed class SQLServerEventSourceRepository(IDbConnection connection) : IEventSourceRepository
{
    private readonly IDbConnection connection = connection;

    public IEnumerable<Event> GetEvents(long afterPosition, int maxCount)
    {
        using var command = connection.CreateCommand($"""
            SELECT TOP {maxCount} Events.*, Entities.type AS entity_type FROM Events
                JOIN Entities ON Entities.id = Events.entity_id
                WHERE position > @position
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
