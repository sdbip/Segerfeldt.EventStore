using System;

namespace Segerfeldt.EventStore.Projection.Tests;

public class EventTests
{
    [Test]
    public void DetailsAsType()
    {
        var @event = new Event("", "", "", @"{""value"":42}", 0, Int64.MinValue);
        var details = @event.DetailsAs<TestDetails>();

        Assert.That(details, Is.EqualTo(new TestDetails(42)));
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record TestDetails(int Value);
}
