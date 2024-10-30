using Segerfeldt.EventStore.Projection.Hosting;

using Segerfeldt.EventStore.Projection.SQLite.Hosting;

using System.Data;

namespace Segerfeldt.EventStore.Projection.SQLite.Tests;

// ReSharper disable once InconsistentNaming
public sealed class ProjectionTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Structure", "NUnit1032:An IDisposable field/property should be Disposed in a TearDown method", Justification = "<Pending>")]
    private InMemoryConnection connection = null!;
    private EventSource eventSource = null!;
    private ReceptacleCollection receptacles = null!;
    private Mock<IPollingStrategy> delayConfiguration = null!;
    private Mock<IProjectionTracker> projectionTracker = null!;

    [SetUp]
    public void Setup()
    {
        connection = new InMemoryConnection();
        delayConfiguration = new Mock<IPollingStrategy>();
        projectionTracker = new Mock<IProjectionTracker>();
        receptacles = new ReceptacleCollection();

        var targetConnection = new Mock<IDbConnection>();
        targetConnection.Setup(c => c.CreateCommand()).Returns(Mock.Of<IDbCommand>());
        targetConnection.Setup(c => c.BeginTransaction()).Returns(Mock.Of<IDbTransaction>());

        eventSource = new EventSource(
            new SQLiteEventSourceRepository(connection),
            new TargetDatabase(() => targetConnection.Object),
            receptacles,
            projectionTracker.Object,
            delayConfiguration.Object);

        SourceDB.Schema.CreateIfMissing(connection);
    }

    [Test]
    public void ReportsEventsWithEntityIdAndDetails()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", @"{""value"":42}");

        var receivedEvents = CaptureReceivedEvents("first-event");

        ProjectionTester.EmitInitialEvents(eventSource);

        Assert.That(receivedEvents, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(receivedEvents[0].EntityId, Is.EqualTo("an-entity"));
            Assert.That(receivedEvents[0].Name, Is.EqualTo("first-event"));
            Assert.That(receivedEvents[0].Details, Is.EqualTo(@"{""value"":42}"));
        });

    }

    [Test]
    public void ReportsEventsOrderedByVersion()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", ordinal: 1);
        GivenEvent("an-entity", "third-event", ordinal: 3);
        GivenEvent("an-entity", "second-event", ordinal: 2);

        var receivedEvents = CaptureReceivedEvents("first-event", "second-event", "third-event");

        ProjectionTester.EmitInitialEvents(eventSource);

        Assert.Multiple(() => {
            Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "first-event", "second-event", "third-event" }));
            Assert.That(receivedEvents.Select(e => e.Name), Is.EqualTo(new[] { "first-event", "second-event", "third-event" }));
        });
    }

    [Test]
    public void NotifiesNewEvents()
    {
        delayConfiguration.Setup(c => c.NextDelay(It.IsAny<int>())).Returns(1);

        GivenEntity("an-entity");
        var receivedEvents = CaptureReceivedEvents("early-event", "late-event");
        GivenEvent("an-entity", "early-event", ordinal: 1, position: 1);
        ProjectionTester.EmitInitialEvents(eventSource);
        receivedEvents.Clear();

        GivenEvent("an-entity", "late-event", ordinal: 2, position: 2);

        ProjectionTester.EmitNewEvents(eventSource);

        Assert.That(receivedEvents, Is.Not.Empty);
        var expected = new[] { "late-event" };
        Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(expected));
    }

    [Test]
    public void AllowsSettingStartPosition()
    {
        GivenEntity("an-entity");
        GivenEvent("an-entity", "first-event", position: 32);
        GivenEvent("an-entity", "second-event", position: 33);
        projectionTracker.Setup(t => t.GetLastFinishedPosition()).Returns(32);

        var receivedEvents = CaptureReceivedEvents("first-event", "second-event");

        ProjectionTester.EmitInitialEvents(eventSource);

        Assert.That(receivedEvents.Select(e => e.Name), Is.EquivalentTo(new[] { "second-event" }));
    }

    [Test]
    public void ReportsNewPosition()
    {
        var finishedPosition = CaptureFinishedPosition();

        GivenEntity("an-entity");
        GivenEvent("an-entity", "an-event", position: 1);
        ProjectionTester.EmitInitialEvents(eventSource);
        Assert.That(finishedPosition.Value, Is.EqualTo(1));
    }

    private void GivenEntity(string entityId)
    {
        var command = connection.CreateCommand("INSERT INTO Entities (id, type, version) VALUES (@entityId, 'a-type', 2)");
        command.AddParameter("@entityId", entityId);
        command.ExecuteNonQuery();
    }

    private void GivenEvent(string entityId, string eventName, string details = "{}", int ordinal = 1, long position = 1)
    {
        var command = connection.CreateCommand(
            @"INSERT INTO Events (entity_id, name, details, actor, ordinal, position)
                    VALUES (@entityId, @eventName, @details, 'test', @ordinal, @position)");
        command.AddParameter("@entityId", entityId);
        command.AddParameter("@eventName", eventName);
        command.AddParameter("@details", details);
        command.AddParameter("@ordinal", ordinal);
        command.AddParameter("@position", position);
        command.ExecuteNonQuery();
    }

    private List<Event> CaptureReceivedEvents(params string[] eventNames)
    {
        var events = new List<Event>();
        foreach (var eventName in eventNames)
            receptacles.Add(new DelegateReceptacle(events.Add, eventName));
        return events;
    }

    private Trap<long> CaptureFinishedPosition()
    {
        var finishedPosition = new Trap<long>();
        projectionTracker.Setup(t => t.OnProjectionFinished(It.IsAny<long>(), It.IsAny<Transaction>()))
            .Callback<long, Transaction>((l, _) => finishedPosition.Value = l);
        return finishedPosition;
    }
}
