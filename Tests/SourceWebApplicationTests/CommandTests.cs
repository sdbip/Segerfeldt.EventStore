using Microsoft.Extensions.DependencyInjection;

using System;
using System.Data.Common;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SourceWebApplicationTests;

public class CommandTests
{
    private readonly JsonSerializerOptions camelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private HttpClient client = null!;
    private WebApplicationFactory<RegisterUser> webApplicationFactory = null!;

    [SetUp]
    public void Setup()
    {
        webApplicationFactory = new WebApplicationFactory<RegisterUser>();
        client = webApplicationFactory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        var connectionPool = webApplicationFactory.Services.GetRequiredService<IConnectionPool>();
        var connection = connectionPool.CreateConnection();
        connection.Open();
        connection.ExecuteNonQuery("DELETE FROM Events; DELETE FROM Entities");
        connection.Close();
    }

    [Test]
    public async Task RegisterUser_Returns204NoContent()
    {
        var response = await PostCommand("User/", new { username = "user4" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    private async Task<HttpResponseMessage> PostCommand(string relativeURL, object command) =>
        await client.PostAsJsonAsync(new Uri(relativeURL, UriKind.Relative), command, camelCase);
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
