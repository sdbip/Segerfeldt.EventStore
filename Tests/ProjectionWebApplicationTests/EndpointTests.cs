using Microsoft.Extensions.DependencyInjection;

using Segerfeldt.EventStore.Projection;
using Segerfeldt.EventStore.Projection.NUnit;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProjectionWebApplicationTests;

public sealed class EndpointTests
{
    private const string EventSourceName = "events";

    private HttpClient client = null!;
    private WebApplicationFactory<ProjectionWebApplication.TestMarker> webApplicationFactory = null!;

    [SetUp]
    public void Setup()
    {
        webApplicationFactory = new();
        client = webApplicationFactory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        var connection = webApplicationFactory.Services.GetRequiredService<TargetDatabase>().CreateConnection();
        var command = connection.CreateCommand("DELETE FROM Players");

        connection.Open();
        try { command.ExecuteNonQuery(); }
        finally { connection.Close(); }
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
        webApplicationFactory.EmitMockEvents(EventSourceName,
            Event("PlayerRegistered", @"{""name"":""Johan""}", ordinal: 0, position: 0),
            Event("ScoreIncreased", @"{""points"":2}", ordinal: 1, position: 0));

        var response = await client.GetAsync(new Uri("Player", UriKind.Relative));

        Assert.Multiple(async () =>
        {
            Assert.That(response.IsSuccessStatusCode, Is.True);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.That(responseBody, Is.EqualTo(@"[{""name"":""Johan"",""score"":2}]"));
        });
    }

    [Test]
    public async Task Projection_NoEvents_ReturnsEmptyBody()
    {
        var response = await client.GetAsync(new Uri("Projection", UriKind.Relative));

        Assert.Multiple(async () =>
        {
            Assert.That(response.IsSuccessStatusCode, Is.True);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.That(responseBody, Is.EqualTo(""));
        });
    }

    [Test]
    public async Task Projection_SingleEvent_ReturnsPosition()
    {
        webApplicationFactory.EmitMockEvents(EventSourceName,
            Event("any", @"{}", ordinal: 0, position: 50));

        var response = await client.GetAsync(new Uri("Projection", UriKind.Relative));

        Assert.Multiple(async () =>
        {
            Assert.That(response.IsSuccessStatusCode, Is.True);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.That(responseBody, Is.EqualTo("50"));
        });
    }

    private static Event Event(string name, string details, int ordinal, long position) =>
        new("a_player", "Player", name, details, ordinal, position);
}
