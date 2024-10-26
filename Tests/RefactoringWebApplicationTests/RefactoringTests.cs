using Microsoft.Extensions.DependencyInjection;

using RefactoringWebApplication;

using Segerfeldt.EventStore.Refactoring;

using System;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RefactoringWebApplicationTests;

public sealed class RefactoringTests
{
    private HttpClient client = null!;
    private WebApplicationFactory<TestMarker> webApplicationFactory = null!;

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
    public async Task Projection_NoEvents_Returns404NotFund()
    {
        var response = await client.GetAsync(new Uri("projection", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task RegisterUser_MovesThePosition()
    {
        await client.SendPostCommand("User/", new { username = "user4" },
            h => h.Authorization = new("Username", "test-user"));
        var eventSource = webApplicationFactory.Services.GetRequiredService<EventSource>();
        eventSource.GetPositionFromTracker();
        eventSource.PollEventsTableOnce();

        var response = await client.GetAsync(new Uri("projection", UriKind.Relative));

        Assert.Multiple(async () =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("0"));
        });
    }
}

internal static class ConnectionExtension
{
    public static void ExecuteNonQuery(this DbConnection connection, string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }
}
