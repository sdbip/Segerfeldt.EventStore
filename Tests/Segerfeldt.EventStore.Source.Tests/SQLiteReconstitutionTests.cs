using NUnit.Framework;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Segerfeldt.EventStore.Source.Tests
{
    // ReSharper disable once InconsistentNaming
    public class SQLiteReconstitutionTests
    {
        private InMemoryConnection connection = null!;
        private EventStore eventStore = null!;

        [SetUp]
        public void Setup()
        {
            connection = new InMemoryConnection();
            eventStore = new EventStore(connection);

            SQLite.SQLite.CreateSchemaIfMissing(connection);
        }

        [Test]
        public void ReconstitutesEntities()
        {
            GivenEntity("an-entity");

            var entity = eventStore.Reconstitute<MyEntity>(new EntityId("an-entity"));

            Assert.That(entity, Is.Not.Null);
        }

        [Test]
        public void ReplaysEvent()
        {
            GivenEntity("an-entity");
            GivenEvent("an-entity", "an-event", @"{""meaning"":42}");

            var entity = eventStore.Reconstitute<MyEntity>(new EntityId("an-entity"));

            Assert.That(entity.ReplayedEvents, Is.Not.Null);
            Assert.That(entity.ReplayedEvents?.Select(e => new
                {
                    e.Name,
                    e.Details
                }),
                Is.EquivalentTo(new[] { new
                {
                    Name = "an-event",
                    Details = @"{""meaning"":42}"
                } }));
        }

        [Test]
        public void ReplaysMultipleEventsInOrder()
        {
            GivenEntity("an-entity");
            GivenEvent("an-entity", "first-event", version: 1);
            GivenEvent("an-entity", "third-event", version: 3);
            GivenEvent("an-entity", "second-event", version: 2);

            var entity = eventStore.Reconstitute<MyEntity>(new EntityId("an-entity"));

            Assert.That(entity.ReplayedEvents, Is.Not.Null);

            var replayedEvents = entity.ReplayedEvents!.ToList();
            Assert.That(replayedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(replayedEvents[1].Name, Is.EqualTo("second-event"));
            Assert.That(replayedEvents[2].Name, Is.EqualTo("third-event"));
        }

        private void GivenEntity(string entityId, int version = 1)
        {
            connection
                .CreateCommand($"INSERT INTO Entities (id, version) VALUES ('{entityId}', {version})")
                .ExecuteNonQuery();
        }

        private void GivenEvent(string entityId, string eventName, string details = "{}", int version = 1)
        {
            connection
                .CreateCommand("INSERT INTO Events (entity, name, details, actor, version, position) " +
                               $"VALUES ('{entityId}', '{eventName}', '{details}', 'test', {version}, 1)")
                .ExecuteNonQuery();
        }

        private class MyEntity : IEntity
        {
            public EntityId Id { get; }
            public EntityVersion Version { get; }
            public IEnumerable<UnpublishedEvent> UnpublishedEvents => ImmutableList<UnpublishedEvent>.Empty;

            public IEnumerable<PublishedEvent>? ReplayedEvents { get; private set; }

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
