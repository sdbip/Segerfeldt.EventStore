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
        private Mock<IDelayConfiguration> delayConfiguration = null!;

        [SetUp]
        public void Setup()
        {
            connection = new InMemoryConnection();
            delayConfiguration = new Mock<IDelayConfiguration>();
            eventSource = new EventSource(connection, delayConfiguration.Object);

            connection
                .CreateCommand("CREATE TABLE Events (entity TEXT, name TEXT, details TEXT, actor TEXT, timestamp INT DEFAULT CURRENT_TIMESTAMP, version INT, position INT)")
                .ExecuteNonQuery();
        }

        [Test]
        public void ReportsEventsWithEntityIdAndDetails()
        {
            GivenEvent("an-entity", "first-event", @"{""value"":42}");

            var projectedEvents = new List<Event>();
            eventSource.AddProjection(projectedEvents.Add);

            eventSource.Start();

            Assert.That(projectedEvents, Is.Not.Empty);
            Assert.That(projectedEvents[0].EntityId, Is.EqualTo("an-entity"));
            Assert.That(projectedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(projectedEvents[0].Details, Is.EqualTo(@"{""value"":42}"));
        }

        [Test]
        public void ReportsEventsOrderedByVersion()
        {
            GivenEvent("an-entity", "first-event", version: 1);
            GivenEvent("an-entity", "third-event", version: 3);
            GivenEvent("an-entity", "second-event", version: 2);

            var projectedEvents = new List<Event>();
            eventSource.AddProjection(projectedEvents.Add);

            eventSource.Start();

            Assert.That(projectedEvents.Count, Is.EqualTo(3));
            Assert.That(projectedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(projectedEvents[1].Name, Is.EqualTo("second-event"));
            Assert.That(projectedEvents[2].Name, Is.EqualTo("third-event"));
        }

        [Test]
        public void AllowsSettingStartingPosition()
        {

        }

        [Test]
        public void ReportsStartingPosition()
        {

        }

        [Test]
        public void NotifiesNewEvents()
        {
            delayConfiguration.Setup(c => c.NextDelay(It.IsAny<int>())).Returns(1);


            var projectedEvents = new List<Event>();
            eventSource.AddProjection(projectedEvents.Add);
            GivenEvent("an-entity", "early-event", version: 1, position: 1);
            eventSource.Start();
            projectedEvents.Clear();

            GivenEvent("an-entity", "late-event", version: 2, position: 2);

            Thread.Sleep(2);

            Assert.That(projectedEvents, Is.Not.Empty);
            Assert.That(projectedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "late-event" }));
        }

        private void GivenEvent(string entityId, string eventName, string details = "{}", int version = 1, long position = 1)
        {
            connection
                .CreateCommand("INSERT INTO Events (entity, name, details, actor, version, position) " +
                               $"VALUES ('{entityId}', '{eventName}', '{details}', 'test', {version}, {position})")
                .ExecuteNonQuery();
        }
    }
}
