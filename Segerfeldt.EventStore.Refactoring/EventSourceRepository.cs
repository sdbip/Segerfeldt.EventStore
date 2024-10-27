using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>A repository that contains the published events of the entities.</summary>
public class EventSourceRepository(IDbConnection connection)
{
    private readonly IDbConnection connection = connection;

    /// <summary>Gets new events sorted chronologically</summary>
    /// <param name="afterPosition">The last position to skip as it has already been processed.</param>
    public IEnumerable<Event> GetEvents(long afterPosition)
    {
        using var command = connection.CreateCommand("""
            SELECT Events.*, Entities.type AS entity_type FROM Events
            JOIN Entities ON Entities.id = Events.entity_id
                WHERE position > @position
            """);
        command.AddParameter("@position", afterPosition);

        connection.Open();
        try { return command.ExecuteReader().AllRowsAs(ReadEvent); }
        catch (DbException) { return []; } // No connection => no events.
        finally { connection.Close(); }
    }

    private static Event ReadEvent(IDataRecord record)
    {
        var entity = new Entity(
            record.GetString(record.GetOrdinal("entity_id")),
            record.GetString(record.GetOrdinal("entity_type")));
        var sourceEvent = new SourceEvent(
            entity,
            record.GetString(record.GetOrdinal("name")),
            record.GetString(record.GetOrdinal("details")),
            Convert.ToInt16(record.GetValue(record.GetOrdinal("ordinal")))
        );
        var metadata = new EventMetadata(
            record.GetInt64(record.GetOrdinal("position")),
            record.GetString(record.GetOrdinal("actor")),
            record.GetDouble(record.GetOrdinal("timestamp"))
        );

        return new(sourceEvent, metadata);
    }
}
