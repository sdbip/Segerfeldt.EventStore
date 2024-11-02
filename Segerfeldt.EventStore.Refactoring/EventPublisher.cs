using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>The object that publishes the transformed events in the target database</summary>
/// <param name="connection">A connection to the target database</param>
public sealed class EventPublisher(IDbConnection connection)
{
    private readonly IDbConnection connection = connection;

    /// <summary>Publish all the transformed events from a given position</summary>
    /// <param name="events">The transformed data</param>
    /// <param name="metadata">The metadata for the position</param>
    public void Publish(IEnumerable<TransformedEvent> events, EventMetadata metadata)
    {
        var entities = events.Select(e => e.Entity).Distinct();

        connection.Open();

        try
        {
            foreach (var entity in entities)
                if (!EntityExists(entity)) InsertEntity(entity);

            foreach (var @event in events)
                InsertEvent(@event);
        }
        finally { connection.Close(); }

        bool EntityExists(Entity entity)
        {
            using var command = connection.CreateCommand("SELECT count(*) FROM Entities WHERE id = @entityId");
            command.AddParameter("@entityId", entity.Id);
            return Convert.ToBoolean(command.ExecuteScalar());
        }

        void InsertEntity(Entity entity)
        {
            using var command = connection.CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@id, @type, 0)");
            command.AddParameter("@id", entity.Id);
            command.AddParameter("@type", entity.Type);
            command.ExecuteNonQuery();
        }

        void InsertEvent(TransformedEvent @event)
        {
            using var command = connection.CreateCommand(
                "INSERT INTO Events (entity_id, name, details, actor, ordinal, position, timestamp)" +
                " VALUES (@entityId, @eventName, @details, @actor, (SELECT COALESCE(MAX(ordinal)+1, 0) FROM Events WHERE entity_id = @entityId), @position, @timestamp)");
            command.AddParameter("@entityId", @event.Entity.Id);
            command.AddParameter("@eventName", @event.Name);
            command.AddParameter("@details", JSON.Serialize(@event.Details));
            command.AddParameter("@actor", metadata.Actor);
            command.AddParameter("@position", metadata.Position);
            command.AddParameter("@timestamp", metadata.Timestamp);
            command.ExecuteNonQuery();
        }
    }
}
