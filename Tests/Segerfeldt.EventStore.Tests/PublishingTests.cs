using Moq;

using NUnit.Framework;

using Segerfeldt.EventStore.Source;
using Segerfeldt.EventStore.Source.SQLite;

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

            new SQLiteConnectionFactory(null!).CreateSchemaIfMissing(connectionFactory.Connection);
        }

        [Test]
        public void CanPublishSingleEvent()
        {
            eventStore.Publish(new EntityId("an-entity"), new UnpublishedEvent("an-event", new{Meaning = 42}), "johan");

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
            connectionFactory.Connection
                .CreateCommand("INSERT INTO Entities (id, version) VALUES ('an-entity', 3)")
                .ExecuteNonQuery();

            var entity = new Mock<IEntity>();
            entity.Setup(e => e.Id).Returns(new EntityId("an-entity"));
            entity.Setup(e => e.Version).Returns(EntityVersion.Of(2));
            entity.Setup(e => e.UnpublishedEvents).Returns(new []{new UnpublishedEvent("an-event", new{})});

            Assert.That(() => eventStore.PublishChanges(entity.Object, "johan"), Throws.Exception);
        }

        private class InMemoryConnectionFactory : IConnectionFactory
        {
            public readonly IDbConnection Connection = new InMemoryConnection();

            public IDbConnection CreateConnection() => Connection;
        }

        private class InMemoryConnection : IDbConnection
        {
            private readonly SQLiteConnection implementor;

            public string ConnectionString
            {
                get => implementor.ConnectionString;
                #nullable disable // This is a fucked up situation!
                set => implementor.ConnectionString = value;
                #nullable enable // The interface is defined as
                                 // string ConnectionString { get; [param: AllowNull] set; }
                                 // What is the point of that!?
            }

            public int ConnectionTimeout => implementor.ConnectionTimeout;
            public string Database => implementor.Database;
            public ConnectionState State => implementor.State;

            public InMemoryConnection() { implementor = new SQLiteConnection("Data Source = :memory:").OpenAndReturn(); }

            public IDbTransaction BeginTransaction() => implementor.BeginTransaction();

            public void Dispose() { implementor.Dispose(); }

            public IDbTransaction BeginTransaction(IsolationLevel il) => throw new System.NotImplementedException();

            public void ChangeDatabase(string databaseName) { implementor.ChangeDatabase(databaseName); }

            public IDbCommand CreateCommand() => implementor.CreateCommand();

            public void Open() { }
            public void Close() { }
        }
    }
}
