using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Projection;

public interface IEventSourceRepository
{
    IEnumerable<Event> GetEvents(long afterPosition);
}

public sealed class DefaultEventSourceRepository(IDbConnection connection) : IEventSourceRepository
{
    private readonly IDbConnection connection = connection;

    public IEnumerable<Event> GetEvents(long afterPosition)
    {
        try
        {
            return connection.OpenAndExecute(_ =>
            {
                using var command = connection.CreateCommand("""
                    SELECT Events.*, Entities.type AS entity_type FROM Events
                    JOIN Entities ON Entities.id = Events.entity_id
                        WHERE position > @position
                    """);
                command.AddParameter("@position", afterPosition);
                return command.ExecuteReader().AllRowsAs(ReadEvent);
            });
        }
        catch (DbException)
        {
            // No connection => no events.
            return [];
        }
    }

    private static Event ReadEvent(IDataRecord record) => new(
        record.GetString(record.GetOrdinal("entity_id")),
        record.GetString(record.GetOrdinal("name")),
        record.GetString(record.GetOrdinal("entity_type")),
        record.GetString(record.GetOrdinal("details")),
        Convert.ToInt16(record.GetValue(record.GetOrdinal("ordinal"))),
        record.GetInt64(record.GetOrdinal("position")));
}
