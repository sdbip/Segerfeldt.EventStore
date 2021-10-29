using Moq;

using NUnit.Framework;

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.Tests
{
    // ReSharper disable once InconsistentNaming
    public class SQLServerProjectionTests
    {
        private SqlConnection connection = null!;
        private EventSource eventSource = null!;
        private Mock<IPollingStrategy> delayConfiguration = null!;
        private Mock<IPositionTracker> positionTracker = null!;

        [SetUp]
        public void Setup()
        {
            connection = new SqlConnection("Server=localhost;Database=test;User Id=sa;Password=S_12345678;");
            delayConfiguration = new Mock<IPollingStrategy>();
            positionTracker = new Mock<IPositionTracker>();

            var connectionPool = new Mock<IConnectionPool>();
            connectionPool.Setup(pool => pool.CreateConnection()).Returns(new SqlConnection("Server=localhost;Database=test;User Id=sa;Password=S_12345678;"));
            eventSource = new EventSource(connectionPool.Object, positionTracker.Object, delayConfiguration.Object);

            delayConfiguration
                .Setup(c => c.NextDelay(It.IsAny<int>()))
                .Returns(Timeout.Infinite);

            connection.Open();
            try
            {
                using var command = connection.CreateCommand("DELETE FROM Events; DELETE FROM Entities;");
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        [TearDown]
        public void TearDown()
        {
            connection.Open();
            try
            {
                using var command = connection.CreateCommand("DELETE FROM Events; DELETE FROM Entities;");
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        [Test]
        public void ReportsEventsWithEntityIdAndDetails()
        {
            GivenEntity("an-entity");
            GivenEvent("an-entity", "first-event", @"{""value"":42}");

            var notifiedEvents = CaptureNotifiedEvents("first-event");

            eventSource.StartReceiving();
            Task.Yield();

            Assert.That(notifiedEvents, Is.Not.Empty);
            Assert.That(notifiedEvents[0].EntityId, Is.EqualTo("an-entity"));
            Assert.That(notifiedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(notifiedEvents[0].Details, Is.EqualTo(@"{""value"":42}"));
        }

        [Test]
        public void ReportsEventsOrderedByVersion()
        {
            GivenEntity("an-entity");
            GivenEvent("an-entity", "first-event", version: 1);
            GivenEvent("an-entity", "third-event", version: 3);
            GivenEvent("an-entity", "second-event", version: 2);

            var notifiedEvents = CaptureNotifiedEvents("first-event", "second-event", "third-event");

            eventSource.StartReceiving();

            Assert.That(notifiedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "first-event", "second-event", "third-event" }));
            Assert.That(notifiedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(notifiedEvents[1].Name, Is.EqualTo("second-event"));
            Assert.That(notifiedEvents[2].Name, Is.EqualTo("third-event"));
        }

        [Test]
        public void NotifiesNewEvents()
        {
            var delay = new[] { 0 };
            delayConfiguration.Setup(c => c.NextDelay(It.IsAny<int>())).Returns(() => delay[0]);

            var notifiedEvents = CaptureNotifiedEvents("an-event");
            GivenEntity("an-entity");

            GivenEvent("an-entity", "an-event", version: 1, position: 1);

            eventSource.StartReceiving();
            notifiedEvents.Clear();

            GivenEvent("an-entity", "an-event", version: 2, position: 2);

            Thread.Sleep(100);

            Assert.That(notifiedEvents.Select(e => e.Position), Is.EquivalentTo(new[] { 2L }));
        }

        [Test]
        public void AllowsSettingStartPosition()
        {
            GivenEntity("an-entity");
            GivenEvent("an-entity", "first-event", position: 32);
            GivenEvent("an-entity", "second-event", position: 33);
            positionTracker.Setup(t => t.GetLastFinishedProjectionId()).Returns(32);

            var notifiedEvents = CaptureNotifiedEvents("first-event", "second-event");

            eventSource.StartReceiving();
            Task.Yield();

            Assert.That(notifiedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "second-event" }));
        }

        [Test]
        public void ReportsNewPosition()
        {
            long? startingPosition = null;
            long? finishedPosition = null;
            positionTracker.Setup(t => t.OnProjectionStarting(It.IsAny<long>()))
                .Callback<long>(l => startingPosition = l);
            positionTracker.Setup(t => t.OnProjectionFinished(It.IsAny<long>()))
                .Callback<long>(l => finishedPosition = l);

            GivenEntity("an-entity");
            GivenEvent("an-entity", "an-event", position: 1);

            eventSource.StartReceiving();
            Task.Yield();

            Assert.That(startingPosition, Is.EqualTo(1));
            Assert.That(finishedPosition, Is.EqualTo(1));
        }

        private void GivenEntity(string entityId, int version = 1)
        {
            connection.Open();
            try
            {
                using var command = connection
                    .CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@entityId, 'a-type', @version)");
                command.AddParameter("@entityId", entityId);
                command.AddParameter("@version", version);
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        private void GivenEvent(string entityId, string eventName, string details = "{}", int version = 1, long position = 1)
        {
            connection.Open();
            try
            {
                using var command = connection.CreateCommand(
                    @"INSERT INTO Events (entity, name, details, actor, version, position)
                    VALUES (@entityId, @eventName, @details, 'test', @version, @position)");
                command.AddParameter("@entityId", entityId);
                command.AddParameter("@eventName", eventName);
                command.AddParameter("@details", details);
                command.AddParameter("@version", version);
                command.AddParameter("@position", position);
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        private List<Event> CaptureNotifiedEvents(params string[] eventNames)
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
