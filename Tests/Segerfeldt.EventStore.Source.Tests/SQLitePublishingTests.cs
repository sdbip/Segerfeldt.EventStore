using Moq;

using NUnit.Framework;

namespace Segerfeldt.EventStore.Source.Tests
{
    // ReSharper disable once InconsistentNaming
    public class SQLitePublishingTests
    {
        private InMemoryConnection connection = null!;
        private EventStore eventStore = null!;

        [SetUp]
        public void Setup()
        {
            connection = new InMemoryConnection();
            eventStore = new EventStore(connection);

            SQLite.Schema.CreateIfMissing(connection);
        }

        [Test]
        public void DoesNotCrashIfSchemaExists()
        {
            SQLite.Schema.CreateIfMissing(connection);
        }

        [Test]
        public void CanPublishSingleEvent()
        {
            eventStore.Publish(new EntityId("an-entity"), new UnpublishedEvent("an-event", new{Meaning = 42}), "johan");

            var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
            reader.Read();

            Assert.That(new
            {
                Entity = reader["entity"],
                Name = reader["name"],
                Details = reader["details"],
                Version = reader["version"],
                Position = reader["position"]
            }, Is.EqualTo(new
            {
                Entity = (object) "an-entity",
                Name = (object) "an-event",
                Details = (object) @"{""meaning"":42}",
                Version = (object) 0,
                Position = (object) 0
            }));
        }

        [Test]
        public void CanPublishChanges()
        {
            var entity = new Mock<IEntity>();
            entity.Setup(e => e.Id).Returns(new EntityId("an-entity"));
            entity.Setup(e => e.Version).Returns(EntityVersion.New);
            entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{Meaning = 42})});
            eventStore.PublishChanges(entity.Object, "johan");

            var reader = connection.CreateCommand("SELECT * FROM Events").ExecuteReader();
            reader.Read();

            Assert.That(new
            {
                Entity = reader["entity"],
                Name = reader["name"],
                Details = reader["details"],
                Version = reader["version"],
                Position = reader["position"]
            }, Is.EqualTo(new
            {
                Entity = (object) "an-entity",
                Name = (object) "an-event",
                Details = (object) @"{""meaning"":42}",
                Version = (object) 0,
                Position = (object) 0
            }));
        }

        [Test]
        public void CannotPublishChangesIfRemoteUpdated()
        {
            connection
                .CreateCommand("INSERT INTO Entities (id, version) VALUES ('an-entity', 3)")
                .ExecuteNonQuery();

            var entity = new Mock<IEntity>();
            entity.Setup(e => e.Id).Returns(new EntityId("an-entity"));
            entity.Setup(e => e.Version).Returns(EntityVersion.Of(2));
            entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{})});

            Assert.That(() => eventStore.PublishChanges(entity.Object, "johan"), Throws.Exception);
        }
    }
}
