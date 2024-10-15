using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

using Segerfeldt.EventStore.Projection.Hosting;
using Segerfeldt.EventStore.Shared;

namespace Segerfeldt.EventStore.Projection;

/// <summary>A repository that contains the published events of the entities.</summary>
public interface IEventSourceRepository
{
    /// <summary>Gets new events sorted chronologically</summary>
    /// <param name="afterPosition">The last position to skip as it has already been processed.</param>
    IEnumerable<Event> GetEvents(long afterPosition);
}

public delegate DbConnection ConnectionFactory();

/// <summary>The default <see cref="IEventSourceRepository"/> implementation</summary>
/// <param name="connectionFactory">A deöegate that can create new connections</param>
public sealed class DefaultEventSourceRepository(ConnectionFactory connectionFactory) : IEventSourceRepository
{
    /// <inheritdoc/>
    public IEnumerable<Event> GetEvents(long afterPosition)
    {
        try
        {
            using var connection = connectionFactory.Invoke();
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
        record.GetString(record.GetOrdinal("entity_type")),
        record.GetString(record.GetOrdinal("name")),
        record.GetString(record.GetOrdinal("details")),
        Convert.ToInt16(record.GetValue(record.GetOrdinal("ordinal"))),
        record.GetInt64(record.GetOrdinal("position")));
}
