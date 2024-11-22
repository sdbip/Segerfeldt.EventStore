using Microsoft.AspNetCore.Mvc.Diagnostics;

using Segerfeldt.EventStore.Source.CommandAPI.DTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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

        var json = await response.Content.ReadAsStringAsync();
        var dtos = JsonSerializer.Deserialize<List<EventDAO>>(json, options)!;
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(dtos, Is.Not.Null);
            Assert.That(dtos, Has.Count.EqualTo(2));
        });
        Assert.Multiple(() =>
        {
            Assert.That(new
            {
                dtos[0].EntityId,
                dtos[0].EntityType,
                dtos[0].Name,
                Details = dtos[0].Details.ToString(),
                dtos[0].Ordinal,
                dtos[0].Position
            }, Is.EqualTo(new
            {
                EntityId = EntityId.Value("user"),
                EntityType = EntityType.Name("User"),
                Name = "Registered",
                Details = "{}",
                Ordinal = Ordinal.Zero,
                Position = Position.Zero
            }));
            Assert.That(new
            {
                dtos[1].EntityId,
                dtos[1].EntityType,
                dtos[1].Name,
                Details = dtos[1].Details.ToString(),
                dtos[1].Ordinal,
                dtos[1].Position
            }, Is.EqualTo(new
            {
                EntityId = EntityId.Value("user"),
                EntityType = EntityType.Name("User"),
                Name = "EmailAddressChanged",
                Details = @"{""emailAddress"":""user@testusers.com""}",
                Ordinal = Ordinal.Of(1),
                Position = Position.Of(1)
            }));
        });
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
        var positionDTOs = JsonSerializer.Deserialize<List<EventDAO>>(json, options)!;
        Assert.That(positionDTOs, Is.Not.Null);
    }
}
