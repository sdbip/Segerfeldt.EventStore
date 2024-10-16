using Microsoft.Extensions.DependencyInjection;

using ProjectionWebApplication;

using Segerfeldt.EventStore.Projection;
using Segerfeldt.EventStore.Projection.Hosting;
using Segerfeldt.EventStore.Projection.NUnit;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProjectionWebApplicationTests;

public sealed class EndpointTests
{
    private HttpClient client = null!;
    private WebApplicationFactory<ScoreBoard> webApplicationFactory = null!;

    [SetUp]
    public void Setup()
    {
        webApplicationFactory = new();
        client = webApplicationFactory.CreateClient();

        var positionTracker = webApplicationFactory.Services.GetService<ProjectionTracker>();
        Assert.That(positionTracker, Is.Not.Null);
        Assert.That(positionTracker!.Position, Is.Null);
    }

    [Test]
    public async Task Player_NoEvents_ReturnsEmptyScoreBoard()
    {
        var response = await client.GetAsync(new Uri("Player", UriKind.Relative));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.That(responseBody, Is.EqualTo("[]"));
    }

    [Test]
    public async Task Player_RegisteredAndIncreased_ReturnsTotalScore()
    {
        Receive(Event("PlayerRegistered", @"{""name"":""Johan""}", ordinal: 0, position: 0),
            Event("ScoreIncreased", @"{""points"":2}", ordinal: 1, position: 0));

        var response = await client.GetAsync(new Uri("Player", UriKind.Relative));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.That(responseBody, Is.EqualTo(@"[{""name"":""Johan"",""score"":2}]"));
    }

    [Test]
    public async Task Projection_NoEvents_ReturnsEmptyBody()
    {
        var response = await client.GetAsync(new Uri("Projection", UriKind.Relative));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.That(responseBody, Is.EqualTo(""));
    }

    [Test]
    public async Task Projection_SingleEvent_ReturnsPosition()
    {
        Receive(Event("any", @"{}", ordinal: 0, position: 50));

        var response = await client.GetAsync(new Uri("Projection", UriKind.Relative));

        Assert.That(response.IsSuccessStatusCode, Is.True);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.That(responseBody, Is.EqualTo("50"));
    }

    private static Event Event(string name, string details, int ordinal, long position) => new("a_player", name, "Player", details, ordinal, position);

    private void Receive(params Event[] events)
    {
        var tester = webApplicationFactory.Services.GetRequiredService<ProjectionTester>();
        tester.Emit("events", events);
    }
}
