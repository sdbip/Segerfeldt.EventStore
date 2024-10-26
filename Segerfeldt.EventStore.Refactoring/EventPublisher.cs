using Segerfeldt.EventStore.Shared;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Segerfeldt.EventStore.Refactoring;

/// <summary>An object that represents the “source of truth” write model of an event-sourced CQRS architecture</summary>
public sealed class EventPublisher
{
    private readonly IDbConnection connection;

    internal EventPublisher(IDbConnection connection)
    {
        this.connection = connection;
    }

    /// <summary>Publish all new changes since reconstituting an entity</summary>
    /// <param name="entities">the entities whose events to publish</param>
    /// <param name="actor">the actor/user who caused these changes</param>
    public void Publish(IEnumerable<TranslatedEvent> events, long position, string actor, double timestamp)
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

        void InsertEvent(TranslatedEvent @event)
        {
            using var command = connection.CreateCommand(
                "INSERT INTO Events (entity_id, name, details, actor, ordinal, position, timestamp)" +
                " VALUES (@entityId, @eventName, @details, @actor, (SELECT COALESCE(MAX(ordinal)+1, 0) FROM Events WHERE entity_id = @entityId), @position, @timestamp)");
            command.AddParameter("@entityId", @event.Entity.Id);
            command.AddParameter("@eventName", @event.Name);
            command.AddParameter("@details", JSON.Serialize(@event.Details));
            command.AddParameter("@actor", actor);
            command.AddParameter("@position", position);
            command.AddParameter("@timestamp", timestamp);
            command.ExecuteNonQuery();
        }
    }
}
