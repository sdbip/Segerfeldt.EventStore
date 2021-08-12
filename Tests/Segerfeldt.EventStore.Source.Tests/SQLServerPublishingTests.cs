using Moq;

using NUnit.Framework;

using System.Data;
using System.Data.SqlClient;

namespace Segerfeldt.EventStore.Source.Tests
{
    public class SQLServerPublishingTests
    {
        private IDbConnection connection = null!;
        private EventStore eventStore = null!;

        [SetUp]
        public void Setup()
        {
            connection = new SqlConnection("Server=localhost;Database=test;User Id=sa;Password=S_12345678;");
            eventStore = new EventStore(connection);

            SQLServer.SQLServer.CreateSchemaIfMissing(connection);
        }

        [TearDown]
        public void TearDown()
        {
            connection.Open();
            connection.CreateCommand("DELETE FROM Events; DELETE FROM Entities;").ExecuteNonQuery();
            connection.Close();
        }

        [Test]
        public void CanPublishSingleEvent()
        {
            eventStore.Publish(new EntityId("an-entity"), new UnpublishedEvent("an-event", new{Meaning = 42}), "johan");

            connection.Open();
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
            connection.Close();
        }

        [Test]
        public void CanPublishChanges()
        {
            var entity = new Mock<IEntity>();
            entity.Setup(e => e.Id).Returns(new EntityId("an-entity"));
            entity.Setup(e => e.Version).Returns(EntityVersion.New);
            entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{Meaning = 42})});
            eventStore.PublishChanges(entity.Object, "johan");

            connection.Open();
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
            connection.Close();
        }

        [Test]
        public void CannotPublishChangesIfRemoteUpdated()
        {
            connection.Open();
            connection
                .CreateCommand("INSERT INTO Entities (id, version) VALUES ('an-entity', 3)")
                .ExecuteNonQuery();
            connection.Close();

            var entity = new Mock<IEntity>();
            entity.Setup(e => e.Id).Returns(new EntityId("an-entity"));
            entity.Setup(e => e.Version).Returns(EntityVersion.Of(2));
            entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{})});

            Assert.That(() => eventStore.PublishChanges(entity.Object, "johan"), Throws.Exception);
        }
    }
}
