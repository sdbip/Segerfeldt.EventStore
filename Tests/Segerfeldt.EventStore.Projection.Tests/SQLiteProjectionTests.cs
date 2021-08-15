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

        [SetUp]
        public void Setup()
        {
            connection = new InMemoryConnection();
            delayConfiguration = new Mock<IPollingStrategy>();
            eventSource = new EventSource(connection, delayConfiguration.Object);

            connection
                .CreateCommand("CREATE TABLE Events (entity TEXT, name TEXT, details TEXT, actor TEXT, timestamp INT DEFAULT CURRENT_TIMESTAMP, version INT, position INT)")
                .ExecuteNonQuery();
        }

        [Test]
        public void ReportsEventsWithEntityIdAndDetails()
        {
            GivenEvent("an-entity", "first-event", @"{""value"":42}");

            var notifiedEvents = CaptureNotifiedEvents();

            eventSource.Start();

            Assert.That(notifiedEvents, Is.Not.Empty);
            Assert.That(notifiedEvents[0].EntityId, Is.EqualTo("an-entity"));
            Assert.That(notifiedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(notifiedEvents[0].Details, Is.EqualTo(@"{""value"":42}"));
        }

        [Test]
        public void ReportsEventsOrderedByVersion()
        {
            GivenEvent("an-entity", "first-event", version: 1);
            GivenEvent("an-entity", "third-event", version: 3);
            GivenEvent("an-entity", "second-event", version: 2);

            var notifiedEvents = CaptureNotifiedEvents();

            eventSource.Start();

            Assert.That(notifiedEvents.Count, Is.EqualTo(3));
            Assert.That(notifiedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(notifiedEvents[1].Name, Is.EqualTo("second-event"));
            Assert.That(notifiedEvents[2].Name, Is.EqualTo("third-event"));
        }

        [Test]
        public void NotifiesNewEvents()
        {
            delayConfiguration.Setup(c => c.NextDelay(It.IsAny<int>())).Returns(1);

            var notifiedEvents = CaptureNotifiedEvents();
            GivenEvent("an-entity", "early-event", version: 1, position: 1);
            eventSource.Start();
            notifiedEvents.Clear();

            GivenEvent("an-entity", "late-event", version: 2, position: 2);

            Thread.Yield();

            Assert.That(notifiedEvents, Is.Not.Empty);
            Assert.That(notifiedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "late-event" }));
        }

        [Test]
        public void AllowsSettingStartPosition()
        {
            GivenEvent("an-entity", "first-event", position: 32);
            GivenEvent("an-entity", "second-event", position: 33);

            var notifiedEvents = CaptureNotifiedEvents();

            eventSource.Start(32);

            Assert.That(notifiedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "second-event" }));
        }

        [Test]
        public void ReportsNewPosition()
        {
            long? position = null;
            eventSource.EventsProcessed += (_, args) => position = args.Position;

            GivenEvent("an-entity", "an-event", position: 1);
            eventSource.Start();

            Assert.That(position, Is.EqualTo(1));
        }

        private void GivenEvent(string entityId, string eventName, string details = "{}", int version = 1, long position = 1)
        {
            connection
                .CreateCommand("INSERT INTO Events (entity, name, details, actor, version, position) " +
                               $"VALUES ('{entityId}', '{eventName}', '{details}', 'test', {version}, {position})")
                .ExecuteNonQuery();
        }

        private List<Event> CaptureNotifiedEvents()
        {
            var events = new List<Event>();
            eventSource.AddProjection(events.Add);
            return events;
        }
    }
}
