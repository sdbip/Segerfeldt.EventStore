using Segerfeldt.EventStore.Source.Internals;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Linq;
using System.Threading;

namespace Segerfeldt.EventStore.Source.PostgreSQL.Tests;

public sealed class EntityStoreTests
{
    private EntityStore store = null!;
    private Mock<IEntityStoreRepository> repository = null!;

    [SetUp]
    public void Setup()
    {
        repository = new Mock<IEntityStoreRepository>();
        store = new EntityStore(repository.Object);
    }

    [Test]
    public void ReconstitutesEntities()
    {
        repository.Setup(r => r.GetHistoryAsync(EntityId.Value("an-entity-1"), It.IsAny<Ordinal>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryDAO { Type = "a-type", Version = 3, Events = [] });

        var entity = store.Reconstitute<MyEntity>(new TypedEntityId("an-entity-1", "a-type"));

        Assert.That(entity, Is.Not.Null);
        Assert.That(entity?.Version, Is.EqualTo(EntityVersion.Of(3)));
    }

    [Test]
    public void ThrowsIfWrongType()
    {
        repository.Setup(r => r.GetHistoryAsync(EntityId.Value("an-entity-1"), It.IsAny<Ordinal>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryDAO { Type = "a-type", Version = 3, Events = [] });

        Assert.That(() => store.Reconstitute<MyEntity>(new TypedEntityId("an-entity-1", "wrong-type")), Throws.Exception);
    }

    [Test]
    public void ReturnsNullIfNoEntity()
    {
        var entity = store.Reconstitute<MyEntity>(new TypedEntityId("an-entity-2", "a-type"));

        Assert.That(entity, Is.Null);
    }

    [Test]
    public void ReplaysEvent()
    {
        repository.Setup(r => r.GetHistoryAsync(EntityId.Value("an-entity-3"), It.IsAny<Ordinal>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryDAO { Type = "a-type", Version = 3, Events = [
                PublishedEvent("an-event", @"{""meaning"":42}")
            ]});

        var entity = store.Reconstitute<MyEntity>(new TypedEntityId("an-entity-3", "a-type"));

        Assert.That(entity?.ReplayedEvents, Is.Not.Null);
        Assert.That(entity?.ReplayedEvents?.Select(e => new
        {
            e.Name,
            e.Details
        }),
            Is.EquivalentTo(new[] { new
            {
                Name = "an-event",
                Details = @"{""meaning"":42}"
            } }));
    }

    [Test]
    public void ReplaysMultipleEventsInOrder()
    {
        repository.Setup(r => r.GetHistoryAsync(EntityId.Value("an-entity-4"), It.IsAny<Ordinal>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryDAO { Type = "a-type", Version = 3, Events = [
                PublishedEvent("first-event", ordinal: 1),
                PublishedEvent("third-event", ordinal: 3),
                PublishedEvent("second-event", ordinal: 2),
            ]});

        var entity = store.Reconstitute<MyEntity>(new TypedEntityId("an-entity-4", "a-type"));

        Assert.That(entity?.ReplayedEvents, Is.Not.Null);

        var replayedEvents = entity!.ReplayedEvents!.ToList();
        Assert.That(replayedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(replayedEvents[1].Name, Is.EqualTo("second-event"));
        Assert.That(replayedEvents[2].Name, Is.EqualTo("third-event"));
    }

    [Test]
    public void CanReadHistoryOnly()
    {
        var timestamp = new DateTimeOffset(2021, 08, 12, 17, 22, 35, TimeSpan.Zero);
        repository.Setup(r => r.GetHistoryAsync(EntityId.Value("an-entity-5"), It.IsAny<Ordinal>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryDAO { Type = "a-type", Version = 3, Events = [
                PublishedEvent("first-event", "johan", timestamp)
            ]});

        var history = store.GetHistory(EntityId.Value("an-entity-5"));

        Assert.That(history, Is.Not.Null);

        var replayedEvents = history!.Events.ToList();
        Assert.That(replayedEvents[0].Actor, Is.EqualTo("johan"));
        Assert.That(replayedEvents[0].Timestamp.UTCDateTime, Is.EqualTo(timestamp).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public void ReadsHistoryInOrder()
    {
        repository.Setup(r => r.GetHistoryAsync(EntityId.Value("an-entity-6"), It.IsAny<Ordinal>(), It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryDAO { Type = "a-type", Version = 3, Events = [
                PublishedEvent("first-event", ordinal: 1),
                PublishedEvent("third-event", ordinal: 3),
                PublishedEvent("second-event", ordinal: 2),
            ]});

        var history = store.GetHistory(EntityId.Value("an-entity-6"));

        Assert.That(history, Is.Not.Null);

        var replayedEvents = history!.Events.ToList();
        Assert.That(replayedEvents[0].Name, Is.EqualTo("first-event"));
        Assert.That(replayedEvents[1].Name, Is.EqualTo("second-event"));
        Assert.That(replayedEvents[2].Name, Is.EqualTo("third-event"));
    }

    private static PublishedEventDAO PublishedEvent(string eventName, string actor, DateTimeOffset timestamp) =>
        new() { Name = eventName, Details = "{}", Actor = actor, Ordinal = 0, Timestamp = Timestamp.FromDateTime(timestamp).Value };

    private static PublishedEventDAO PublishedEvent(string eventName, string details = "{}", int ordinal = 0) =>
        new() { Name = eventName, Details = details, Ordinal = ordinal, Actor = "test", Timestamp = 0 };

    // ReSharper disable once ClassNeverInstantiated.Local
    private class MyEntity(EntityId id, EntityVersion version) : IEntity
    {
        public EntityId Id { get; } = id;
        public EntityVersion Version { get; } = version;
        public EntityType Type => EntityType.Name("MyEntity");
        public IEnumerable<UnpublishedEvent> UnpublishedEvents => ImmutableList<UnpublishedEvent>.Empty;

        public IEnumerable<PublishedEvent>? ReplayedEvents { get; private set; }

        public void ReplayEvents(IEnumerable<PublishedEvent> events)
        {
            ReplayedEvents = events;
        }
    }
}
