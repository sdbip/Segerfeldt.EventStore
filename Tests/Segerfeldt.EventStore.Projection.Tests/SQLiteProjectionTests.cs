using Moq;

using NUnit.Framework;

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Segerfeldt.EventStore.Projection.Tests
{
    // ReSharper disable once InconsistentNaming
    public class SQLiteProjectionTests
    {
        private InMemoryConnection connection = null!;
        private EventSource eventSource = null!;
        private Mock<IPollingStrategy> delayConfiguration = null!;
        private Mock<IPositionTracker> positionTracker = null!;

        [SetUp]
        public void Setup()
        {
            connection = new InMemoryConnection();
            var connectionPool = new Mock<IConnectionPool>();
            connectionPool.Setup(pool => pool.CreateConnection()).Returns(connection);
            delayConfiguration = new Mock<IPollingStrategy>();
            positionTracker = new Mock<IPositionTracker>();
            eventSource = new EventSource(connectionPool.Object, positionTracker.Object, delayConfiguration.Object);

            connection
                .CreateCommand("CREATE TABLE Entities (id TEXT, type TEXT, version INT);" +
                               "CREATE TABLE Events (entity TEXT, name TEXT, details TEXT, actor TEXT, timestamp INT DEFAULT CURRENT_TIMESTAMP, version INT, position INT)")
                .ExecuteNonQuery();
        }

        [Test]
        public void ReportsEventsWithEntityIdAndDetails()
        {
            GivenEntity("an-entity");
            GivenEvent("an-entity", "first-event", @"{""value"":42}");

            var receivedEvents = CaptureReceivedEvents("first-event");

            eventSource.StartReceiving();

            Assert.That(receivedEvents, Is.Not.Empty);
            Assert.That(receivedEvents[0].EntityId, Is.EqualTo("an-entity"));
            Assert.That(receivedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(receivedEvents[0].Details, Is.EqualTo(@"{""value"":42}"));
        }

        [Test]
        public void ReportsEventsOrderedByVersion()
        {
            GivenEntity("an-entity");
            GivenEvent("an-entity", "first-event", version: 1);
            GivenEvent("an-entity", "third-event", version: 3);
            GivenEvent("an-entity", "second-event", version: 2);

            var receivedEvents = CaptureReceivedEvents("first-event", "second-event", "third-event");

            eventSource.StartReceiving();

            Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "first-event", "second-event", "third-event" }));
            Assert.That(receivedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(receivedEvents[1].Name, Is.EqualTo("second-event"));
            Assert.That(receivedEvents[2].Name, Is.EqualTo("third-event"));
        }

        [Test]
        public void NotifiesNewEvents()
        {
            delayConfiguration.Setup(c => c.NextDelay(It.IsAny<int>())).Returns(1);

            GivenEntity("an-entity");
            var receivedEvents = CaptureReceivedEvents("early-event", "late-event");
            GivenEvent("an-entity", "early-event", version: 1, position: 1);
            eventSource.StartReceiving();
            receivedEvents.Clear();

            GivenEvent("an-entity", "late-event", version: 2, position: 2);

            Thread.Yield();

            Assert.That(receivedEvents, Is.Not.Empty);
            Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "late-event" }));
        }

        [Test]
        public void AllowsSettingStartPosition()
        {
            GivenEntity("an-entity");
            GivenEvent("an-entity", "first-event", position: 32);
            GivenEvent("an-entity", "second-event", position: 33);
            positionTracker.Setup(t => t.GetLastFinishedProjectionId()).Returns(32);

            var receivedEvents = CaptureReceivedEvents("first-event", "second-event");

            eventSource.StartReceiving();

            Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "second-event" }));
        }

        [Test]
        public void ReportsNewPosition()
        {
            long? startingPosition = null;
            long? finishedPosition = null;
            positionTracker.Setup(t => t.OnProjectionFinished(It.IsAny<long>()))
                .Callback<long>(l => startingPosition = l);
            positionTracker.Setup(t => t.OnProjectionStarting(It.IsAny<long>()))
                .Callback<long>(l => finishedPosition = l);

            GivenEntity("an-entity");
            GivenEvent("an-entity", "an-event", position: 1);
            eventSource.StartReceiving();

            Assert.That(startingPosition, Is.EqualTo(1));
            Assert.That(finishedPosition, Is.EqualTo(1));
        }

        private void GivenEntity(string entityId)
        {
            connection
                .CreateCommand("INSERT INTO Entities (id, type, version) " +
                               $"VALUES ('{entityId}', 'a-type', 2)")
                .ExecuteNonQuery();
        }

        private void GivenEvent(string entityId, string eventName, string details = "{}", int version = 1, long position = 1)
        {
            connection
                .CreateCommand("INSERT INTO Events (entity, name, details, actor, version, position) " +
                               $"VALUES ('{entityId}', '{eventName}', '{details}', 'test', {version}, {position})")
                .ExecuteNonQuery();
        }

        private List<Event> CaptureReceivedEvents(params string[] eventNames)
        {
            var events = new List<Event>();
            foreach (var eventName in eventNames)
            {
                eventSource.Register(new DelegateReceptacle(events.Add, eventName));
            }

            return events;
        }
    }
}
