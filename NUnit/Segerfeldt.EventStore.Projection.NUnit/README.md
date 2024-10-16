# Segerfeldt.EventStore.Projection.NUnit

A package for assisting tests of Segerfeldt.EventSourcing.Projection applications.

If you set up your `EventSource` with a name, you can mock events emitted from that source in integration tests:

```csharp
builder.Services.AddHostedEventSource(new MyEventSourceProvider(), "main-source")
    .AddReceptacles(...);
```

Use the `WebApplicationFactory<T>.CreateClient()` and `EmitMockEvents()` to run integration tests.

```csharp
using Segerfeldt.EventStore.Projection.NUnit;

namespace ProjectionWebApplicationTests;

public sealed class EndpointTests
{
    private HttpClient client = null!;
    private WebApplicationFactory<ProjectionWebApplication.AnyClass> webApplicationFactory = null!;

    [SetUp]
    public void Setup()
    {
        webApplicationFactory = new();
        client = webApplicationFactory.CreateClient();
    }

    [Test]
    public async Task Player_RegisteredAndIncreased_ReturnsTotalScore()
    {
        webApplicationFactory.EmitMockEvents("main-source",
            new("a_player", "Player", "PlayerRegistered", @"{""name"":""Johan""}", ordinal: 0, position: 0),
            new("a_player", "Player", "ScoreIncreased", @"{""points"":2}", ordinal: 1, position: 0));

        var response = await client.GetAsync(new Uri("Player", UriKind.Relative));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.That(responseBody, Is.EqualTo(@"[{""name"":""Johan"",""score"":2}]"));
    }
}
```
