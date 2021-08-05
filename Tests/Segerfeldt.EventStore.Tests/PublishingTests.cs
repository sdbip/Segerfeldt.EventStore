using Moq;

using NUnit.Framework;

using Segerfeldt.EventStore.Source;

using System.Data;
using System.Data.SQLite;

namespace Segerfeldt.EventStore.Tests
{
    public class PublishingTests
    {
        private InMemoryConnectionFactory connectionFactory = null!;
        private Source.EventStore eventStore = null!;

        [SetUp]
        public void Setup()
        {
            connectionFactory = new InMemoryConnectionFactory();
            eventStore = new Source.EventStore(connectionFactory);

            var connection = connectionFactory.CreateConnection();
            connection.Open();
            new SqLiteConnectionFactory(null!).CreateSchemaIfMissing(connection);
        }

        [Test]
        public void CanPublishSingleEvent()
        {
            eventStore.Publish(new EntityId("test"), new UnpublishedEvent("test", new{Meaning = 42}), "johan");

            var reader = connectionFactory.CreateConnection().CreateCommand("SELECT * FROM Events").ExecuteReader();
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
                Entity = (object) "test",
                Name = (object) "test",
                Details = (object) @"{""meaning"":42}",
                Version = (object) 1,
                Position = (object) 1
            }));
        }

        [Test]
        public void CanPublishChanges()
        {
            var entity = new Mock<IEntity>();
            entity.Setup(e => e.Id).Returns(new EntityId("test"));
            entity.Setup(e => e.Version).Returns(EntityVersion.New);
            entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("test", new{Meaning = 42})});
            eventStore.PublishChanges(entity.Object, "johan");

            var reader = connectionFactory.CreateConnection().CreateCommand("SELECT * FROM Events").ExecuteReader();
            reader.Read();

            Assert.That(reader["entity"], Is.EqualTo("test"));
            Assert.That(reader["name"], Is.EqualTo("test"));
            Assert.That(reader["details"], Is.EqualTo(@"{""meaning"":42}"));
            Assert.That(reader["version"], Is.EqualTo(1));
            Assert.That(reader["position"], Is.EqualTo(1));
        }

        [Test]
        public void CannotPublishChangesIfRemoteUpdated()
        {
            connectionFactory.CreateConnection().CreateCommand(
                "INSERT INTO Events (entity, name, details, actor, version, position)" +
                " VALUES ('test', 'previous-event, '{}', 'system', 3, 3)");

            var entity = new Mock<IEntity>();
            entity.Setup(e => e.Id).Returns(new EntityId("test"));
            entity.Setup(e => e.Version).Returns(EntityVersion.Of(2));
            entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("test", new{Meaning = 42})});

            Assert.That(() => eventStore.PublishChanges(entity.Object, "johan"), Throws.Exception);
        }

        private class InMemoryConnectionFactory : IConnectionFactory
        {
            private readonly IDbConnection connection = new SQLiteConnection("Data Source = :memory:");
            public IDbConnection CreateConnection() => connection;
        }
    }
}
