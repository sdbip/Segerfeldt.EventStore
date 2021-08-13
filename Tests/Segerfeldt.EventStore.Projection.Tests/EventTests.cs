using NUnit.Framework;

namespace Segerfeldt.EventStore.Projection.Tests
{
    public class EventTests
    {
        [Test]
        public void DetailsAsType()
        {
            var @event = new Event("", "", @"{""value"":42}");
            var details = @event.DetailsAs<TestDetails>();

            Assert.That(details, Is.EqualTo(new TestDetails(42)));
        }

        private record TestDetails(int Value);
    }
}
