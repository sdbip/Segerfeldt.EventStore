using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.Tests;

public sealed class ReceptacleBaseTests
{
    [Test]
    public void InvokesMethodWithMatchingEventNameAndType()
    {
        var receptacle = new EntityTypeTestingReceptacle();
        receptacle.Update(new Event("an-entity", EntityTypeTestingReceptacle.MatchedType, EntityTypeTestingReceptacle.WhereReceptacleSpecifiesType, "{}", 0, 0));

        Assert.That(receptacle.ReceivedEvent, Is.Not.Null);
    }

    [Test]
    public void DoesNotInvokeMethodWithMismatchingEventType()
    {
        var receptacle = new EntityTypeTestingReceptacle();
        receptacle.Update(new Event("an-entity", "mismatching-type", EntityTypeTestingReceptacle.WhereReceptacleSpecifiesType, "{}", 0, 0));

        Assert.That(receptacle.ReceivedEvent, Is.Null);
    }

    [Test]
    public void InvokesMethodIfEventTypeIgnored()
    {
        var receptacle = new EntityTypeTestingReceptacle();
        receptacle.Update(new Event("an-entity", "an-entity-type", EntityTypeTestingReceptacle.WhereReceptacleIgnoresType, "{}", 0, 0));

        Assert.That(receptacle.ReceivedEvent, Is.Not.Null);
    }

    [Test]
    public void InvokesMethodWithOnlyEventParameter()
    {
        var receptacle = new ParameterListTestingReceptacle();
        receptacle.Update(new Event("an-entity", "an-entity-type", ParameterListTestingReceptacle.WhereReceptacleAcceptsEventOnly, "{}", 0, 0));

        Assert.That(receptacle.ReceivedEvent, Is.Not.Null);
    }

    [Test]
    public void InvokesMethodWithEntityIdAndDataParameters()
    {
        var receptacle = new ParameterListTestingReceptacle();
        receptacle.Update(new Event("an-entity", "an-entity-type", ParameterListTestingReceptacle.WhereReceptacleAcceptsIdAndData, @"{""property"":42}", 0, 0));

        Assert.That(receptacle.ReceivedEntityId, Is.EqualTo("an-entity"));
        Assert.That(receptacle.ReceivedData, Is.EqualTo(new EventData(42)));
    }

    [Test]
    public void ExecutesTasksSynchronously()
    {
        var receptacle = new AsyncTestingReceptacle();
        receptacle.Update(new Event("an-entity", "an-entity-type", AsyncTestingReceptacle.AcceptedEvent, "{}", 0, 0));
    }
}

public class EntityTypeTestingReceptacle : ReceptacleBase
{
    public const string MatchedType = "an-entity-type";
    public const string WhereReceptacleIgnoresType = "event_without_type";
    public const string WhereReceptacleSpecifiesType = "event_with_type";

    public Event? ReceivedEvent { private set; get; }

    [ReceivesEvent(WhereReceptacleIgnoresType)]
    public void ReceiveEventWithoutType(Event @event)
    {
        ReceivedEvent = @event;
    }

    [ReceivesEvent(WhereReceptacleSpecifiesType, EntityType = MatchedType)]
    public void ReceiveEventWithType(Event @event)
    {
        ReceivedEvent = @event;
    }
}

public class ParameterListTestingReceptacle : ReceptacleBase
{
    public const string WhereReceptacleAcceptsEventOnly = "WhereReceptacleAcceptsEventOnly";
    public const string WhereReceptacleAcceptsIdAndData = "WhereReceptacleAcceptsIdAndData";

    public Event? ReceivedEvent { private set; get; }
    public string? ReceivedEntityId { get; private set; }
    public EventData? ReceivedData { get; private set; }

    [ReceivesEvent(WhereReceptacleAcceptsEventOnly)]
    public void ReceiveEventWithoutType(Event @event)
    {
        ReceivedEvent = @event;
    }

    [ReceivesEvent(WhereReceptacleAcceptsIdAndData)]
    public void ReceiveEventWithType(string entityId, EventData data)
    {
        ReceivedEntityId = entityId;
        ReceivedData = data;
    }
}

public class AsyncTestingReceptacle : ReceptacleBase
{
    public const string AcceptedEvent = "AcceptedEvent";

    private bool isProcessingEvent;

    [ReceivesEvent(AcceptedEvent)]
    public async Task Method1(Event _){ await WaitAsync(); }

    [ReceivesEvent(AcceptedEvent)]
    public async Task Method2(Event _){ await WaitAsync(); }

    [ReceivesEvent(AcceptedEvent)]
    public async Task Method3(Event _){ await WaitAsync(); }

    private async Task WaitAsync()
    {
        Assert.That(isProcessingEvent, Is.False);
        isProcessingEvent = true;
        await Task.Delay(10);
        isProcessingEvent = false;
    }
}

public record EventData(int Property);
