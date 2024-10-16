using Segerfeldt.EventStore.Source.Snapshots;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Segerfeldt.EventStore.Shared;
using Segerfeldt.EventStore.Tests.Shared;

namespace Segerfeldt.EventStore.Source.Tests;

// ReSharper disable once InconsistentNaming
public sealed class SQLiteSnapshotTests
{
    private InMemoryConnection connection = null!;
    private EntityStore store = null!;

    [SetUp]
    public void Setup()
    {
        connection = new InMemoryConnection();
        store = new EntityStore(connection);

        SQLite.Schema.CreateIfMissing(connection);
    }

    [Test]
    public void CreatesEntityObject()
    {
        GivenEntity("an-entity-1", "a-type", 42);

        var snapshot = new Snapshot(new EntityId("an-entity-1"), new EntityType("a-type"), EntityVersion.Of(13)) {Value = 19};
        var entity = store.Reconstitute(snapshot);

        Assert.That(new {entity?.Id, entity?.Version, entity?.SnapshotValue},
            Is.EqualTo(new {Id = new EntityId("an-entity-1"), Version = EntityVersion.Of(42), SnapshotValue = (int?)19}));
    }

    [Test]
    public void ReplaysPublishedEvents()
    {
        GivenEntity("an-entity-2", "a-type");
        GivenEvent("an-entity-2", "at-snapshot-event", ordinal: 42);
        GivenEvent("an-entity-2", "after-snapshot-event", ordinal: 43);

        var snapshot = new Snapshot(new EntityId("an-entity-2"), new EntityType("a-type"), EntityVersion.Of(42));
        var entity = store.Reconstitute(snapshot);

        Assert.That(entity?.ReplayedEvents?.Select(e => e.Name),
            Is.EquivalentTo(new[] { "after-snapshot-event" }));
    }

    private void GivenEntity(string entityId, string entityType, int version = 1)
    {
        var command = connection.CreateCommand(
            "INSERT INTO Entities (id, type, version) VALUES (@entityId, @entityType, @version)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@entityType", entityType);
        command.AddParameter("@version", version);
        command.ExecuteNonQuery();
    }

    private void GivenEvent(string entityId, string eventName, string details = "{}", int ordinal = 1)
    {
        var command = connection.CreateCommand(
            @"INSERT INTO Events (entity_id, name, details, actor, ordinal, position)
                    VALUES (@entityId, @eventName, @details, 'test', @ordinal, 1)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@eventName", eventName);
        command.AddParameter("@details", details);
        command.AddParameter("@ordinal", ordinal);
        command.ExecuteNonQuery();
    }

    private class Snapshot(EntityId id, EntityType entityType, EntityVersion version) : ISnapshot<MyEntity>
    {
        public EntityId Id { get; } = id;
        public EntityType EntityType { get; } = entityType;
        public EntityVersion Version { get; } = version;

        public int Value { get; init; }

        public void Restore(MyEntity entity)
        {
            entity.SnapshotValue = Value;
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class MyEntity(EntityId id, EntityVersion version) : IEntity
    {
        public EntityId Id { get; } = id;
        public EntityVersion Version { get; } = version;
        public EntityType Type => new("MyEntity");
        public IEnumerable<UnpublishedEvent> UnpublishedEvents => ImmutableList<UnpublishedEvent>.Empty;

        public IEnumerable<PublishedEvent>? ReplayedEvents { get; private set; }
        public int? SnapshotValue { get; set; }

        public void ReplayEvents(IEnumerable<PublishedEvent> events)
        {
            ReplayedEvents = events;
        }
    }
}
