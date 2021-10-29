using NUnit.Framework;

using Segerfeldt.EventStore.Source.Snapshots;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Segerfeldt.EventStore.Source.Tests
{
    // ReSharper disable once InconsistentNaming
    public class SQLiteSnapshotTests
    {
        private InMemoryConnection connection = null!;
        private EntityStore store = null!;

        [SetUp]
        public void Setup()
        {
            connection = new InMemoryConnection();
            var connectionPool = new SingletonConnectionPool(connection);
            store = new EntityStore(connectionPool);

            SQLite.Schema.CreateIfMissing(connection);
        }

        [Test]
        public void CreatesEntityObject()
        {
            GivenEntity("an-entity", "a-type", 42);

            var snapshot = new Snapshot(new EntityId("an-entity"), new EntityType("a-type"), EntityVersion.Of(13)) {Value = 19};
            var entity = store.Reconstitute(snapshot);

            Assert.That(new {entity?.Id, entity?.Version, entity?.SnapshotValue},
                Is.EqualTo(new {Id = new EntityId("an-entity"), Version = EntityVersion.Of(42), SnapshotValue = (int?)19}));
        }

        [Test]
        public void ReplaysPublishedEvents()
        {
            GivenEntity("an-entity", "a-type");
            GivenEvent("an-entity", "at-snapshot-event", version: 42);
            GivenEvent("an-entity", "after-snapshot-event", version: 43);

            var snapshot = new Snapshot(new EntityId("an-entity"), new EntityType("a-type"), EntityVersion.Of(42));
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

        private void GivenEvent(string entityId, string eventName, string details = "{}", int version = 1)
        {
            var command = connection.CreateCommand(
                @"INSERT INTO Events (entity, name, details, actor, version, position)
                    VALUES (@entityId, @eventName, @details, 'test', @version, 1)");
            command.AddParameter("@entityId", entityId);
            command.AddParameter("@eventName", eventName);
            command.AddParameter("@details", details);
            command.AddParameter("@version", version);
            command.ExecuteNonQuery();
        }

        private class Snapshot : ISnapshot<MyEntity>
        {
            public EntityId Id { get; }
            public EntityType EntityType { get; }
            public EntityVersion Version { get; }

            public int Value { get; init; }

            public Snapshot(EntityId id, EntityType entityType, EntityVersion version)
            {
                Id = id;
                EntityType = entityType;
                Version = version;
            }

            public void Restore(MyEntity entity)
            {
                entity.SnapshotValue = Value;
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class MyEntity : IEntity
        {
            public EntityId Id { get; }
            public EntityVersion Version { get; }
            public EntityType Type => new("MyEntity");
            public IEnumerable<UnpublishedEvent> UnpublishedEvents => ImmutableList<UnpublishedEvent>.Empty;

            public IEnumerable<PublishedEvent>? ReplayedEvents { get; private set; }
            public int? SnapshotValue { get; set; }

            public MyEntity(EntityId id, EntityVersion version)
            {
                Id = id;
                Version = version;
            }

            public void ReplayEvents(IEnumerable<PublishedEvent> events)
            {
                ReplayedEvents = events;
            }
        }
    }
}
