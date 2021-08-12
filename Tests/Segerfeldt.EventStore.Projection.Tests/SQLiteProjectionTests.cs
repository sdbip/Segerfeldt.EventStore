using NUnit.Framework;

using System.Collections.Generic;

namespace Segerfeldt.EventStore.Projection.Tests
{
    // ReSharper disable once InconsistentNaming
    public class SQLiteProjectionTests
    {
        private InMemoryConnection connection = null!;
        private EventSource eventSource = null!;

        [SetUp]
        public void Setup()
        {
            connection = new InMemoryConnection();
            eventSource = new EventSource(connection);

            connection
                .CreateCommand("CREATE TABLE Events (entity TEXT, name TEXT, details TEXT, actor TEXT, timestamp INT DEFAULT CURRENT_TIMESTAMP, version INT, position INT)")
                .ExecuteNonQuery();
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

        private void GivenEvent(string entityId, string eventName, string details = "{}", int version = 1)
        {
            connection
                .CreateCommand("INSERT INTO Events (entity, name, details, actor, version, position) " +
                               $"VALUES ('{entityId}', '{eventName}', '{details}', 'test', {version}, 1)")
                .ExecuteNonQuery();
        }
    }
}
