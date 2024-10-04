using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Segerfeldt.EventStore.Projection;

public interface IEventSourceRepository
{
    IEnumerable<Event> GetEvents(long afterPosition);
}

public sealed class DefaultEventSourceRepository : IEventSourceRepository
{
    private readonly IDbConnection connection;

    public DefaultEventSourceRepository(IConnectionPool connectionPool)
    {
        connection = connectionPool.CreateConnection();
    }

    public IEnumerable<Event> GetEvents(long afterPosition)
    {
        try
        {
            return connection.OpenAndExecute(_ =>
            {
                var command = connection.CreateCommand("""
                    SELECT Events.*, Entities.type AS entity_type FROM Events
                    JOIN Entities ON Entities.id = Events.entity_id
                        WHERE position > @position
                        ORDER BY position, version
                    """);
                command.AddParameter("@position", afterPosition);
                return command.ExecuteReader().AllRowsAs(ReadEvent);
            });
        }
        catch (DbException)
        {
            // No connection => no events.
            return Array.Empty<Event>();
        }
    }

    private static Event ReadEvent(IDataRecord record) => new(
        record.GetString(record.GetOrdinal("entity_id")),
        record.GetString(record.GetOrdinal("name")),
        record.GetString(record.GetOrdinal("entity_type")),
        record.GetString(record.GetOrdinal("details")),
        record.GetInt64(record.GetOrdinal("position")));
}
