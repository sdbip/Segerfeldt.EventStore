using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace Segerfeldt.EventStore.Projection.Tests
{
    public class ReceptacleBaseTests
    {
        [Test]
        public void InvokesMethodWithMatchingEventNameAndType()
        {
            var receptacle = new EntityTypeTestingReceptacle();
            receptacle.ReceiveAsync(new Event("an-entity", EntityTypeTestingReceptacle.WhereReceptacleSpecifiesType, EntityTypeTestingReceptacle.MatchedType, "{}", 0));

            Assert.That(receptacle.ReceivedEvent, Is.Not.Null);
        }

        [Test]
        public void DoesNotInvokeMethodWithMismatchingEventType()
        {
            var receptacle = new EntityTypeTestingReceptacle();
            receptacle.ReceiveAsync(new Event("an-entity", EntityTypeTestingReceptacle.WhereReceptacleSpecifiesType, "mismatching-type", "{}", 0));

            Assert.That(receptacle.ReceivedEvent, Is.Null);
        }

        [Test]
        public void InvokesMethodIfEventTypeIgnored()
        {
            var receptacle = new EntityTypeTestingReceptacle();
            receptacle.ReceiveAsync(new Event("an-entity", EntityTypeTestingReceptacle.WhereReceptacleIgnoresType, "an-entity-type", "{}", 0));

            Assert.That(receptacle.ReceivedEvent, Is.Not.Null);
        }

        [Test]
        public void InvokesMethodWithOnlyEventParameter()
        {
            var receptacle = new ParameterListTestingReceptacle();
            receptacle.ReceiveAsync(new Event("an-entity", ParameterListTestingReceptacle.WhereReceptacleAcceptsEventOnly, "an-entity-type", "{}", 0));

            Assert.That(receptacle.ReceivedEvent, Is.Not.Null);
        }

        [Test]
        public void InvokesMethodWithOnlyEntityIdAndSDataParameters()
        {
            var receptacle = new ParameterListTestingReceptacle();
            receptacle.ReceiveAsync(new Event("an-entity", ParameterListTestingReceptacle.WhereReceptacleAcceptsIdAndData, "an-entity-type", @"{""property"":42}", 0));

            Assert.That(receptacle.ReceivedEntityId, Is.EqualTo("an-entity"));
            Assert.That(receptacle.ReceivedData, Is.EqualTo(new EventData(42)));
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

    public record EventData(int Property);
}
