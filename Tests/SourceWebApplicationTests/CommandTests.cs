using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SourceWebApplicationTests;

public sealed class CommandTests
{
    private HttpClient client = null!;
    private WebApplicationFactory<SourceWebApplication.TestMarker> webApplicationFactory = null!;

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
    public async Task RegisterUser_Authenticated_Returns204NoContent()
    {
        var response = await client.SendPostCommand("User/", new { username = "user4" },
            h => h.Authorization = new("Username", "test-user"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task RegisterUser_NotAuthenticated_Returns401Unauthorized()
    {
        var response = await client.SendPostCommand("User/", new { username = "user4" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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
