using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SourceWebApplicationTests;

public sealed class EndpointTests
{
    private readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    private HttpClient client = null!;
    private WebApplicationFactory<RegisterUser> webApplicationFactory = null!;

    [SetUp]
    public void Setup()
    {
        webApplicationFactory = new();
        client = webApplicationFactory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        webApplicationFactory.ClearSourceTables();
    }

    [Test]
    public async Task EntityHistory_UnknownId_Returns404NotFound()
    {
        var response = await client.GetAsync(new Uri("history/2", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task EntityHistory_UserRegistered_ReturnsEvent()
    {
        await client.SendPostCommand("User", new RegisterUser(EntityId.Value("user")),
            h => h.Authorization = new("Username", "test-user"));

        var response = await client.GetAsync(new Uri("history/user", UriKind.Relative));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadAsStringAsync();
        var history = JsonSerializer.Deserialize<History>(json, options);
        var events = history?.Events.ToList();

        Assert.That(history, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(history?.Type, Is.EqualTo("User"));
            Assert.That(history?.Version, Is.EqualTo(0));
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events?[0].Name, Is.EqualTo("Registered"));
            Assert.That(events?[0].Details, Is.TypeOf<JsonElement>()); //"{}"));
            Assert.That(((JsonElement?)events?[0].Details).ToString(), Is.EqualTo("{}"));
        });
    }

    [Test]
    public async Task GlobalHistory_AfterTooHighPosition_ReturnsEmptyList()
    {
        var response = await client.GetAsync(new Uri("history?after=42", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("[]"));
    }

    [Test]
    public async Task GlobalHistory_EventsExist_ReturnsCompleteHistory()
    {
        await client.SendPostCommand("User", new RegisterUser(EntityId.Value("user")),
            h => h.Authorization = new("Username", "test-user"));
        await client.SendPostCommand("User/user/emailAddress", new SetEmailAddress("user@testusers.com"),
            h => h.Authorization = new("Username", "test-user"));

        var response = await client.GetAsync(new Uri("history", UriKind.Relative));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadAsStringAsync();
        var positionDTOs = JsonSerializer.Deserialize<List<ProjectionPosition>>(json, options)!;
        Assert.That(positionDTOs, Is.Not.Null);
        Assert.That(positionDTOs, Has.Count.EqualTo(2));
        Assert.That(positionDTOs[0].Position, Is.EqualTo(0));
        Assert.That(positionDTOs[1].Position, Is.EqualTo(1));
        Assert.That(positionDTOs[0].Events, Has.Count.EqualTo(1));
        Assert.That(positionDTOs[1].Events, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GlobalHistory_AfterEmptyString_ReturnsCompleteHistory()
    {
        await client.SendPostCommand("User", new RegisterUser(EntityId.Value("user")),
            h => h.Authorization = new("Username", "test-user"));
        await client.SendPostCommand("User/user/emailAddress", new SetEmailAddress("user@testusers.com"),
            h => h.Authorization = new("Username", "test-user"));

        var response = await client.GetAsync(new Uri("history?after=", UriKind.Relative));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await response.Content.ReadAsStringAsync();
        var positionDTOs = JsonSerializer.Deserialize<List<ProjectionPosition>>(json, options)!;
        Assert.That(positionDTOs, Is.Not.Null);
        Assert.That(positionDTOs, Has.Count.EqualTo(2));
        Assert.That(positionDTOs[0].Position, Is.EqualTo(0));
        Assert.That(positionDTOs[1].Position, Is.EqualTo(1));
        Assert.That(positionDTOs[0].Events, Has.Count.EqualTo(1));
        Assert.That(positionDTOs[1].Events, Has.Count.EqualTo(2));
    }
}
