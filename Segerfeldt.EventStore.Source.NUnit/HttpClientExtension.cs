using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.NUnit;

public static class HttpClientExtension
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<HttpResponseMessage> SendPostCommand(this HttpClient client, string relativeURL, object command) =>
        await client.SendCommand(HttpMethod.Post, relativeURL, command);

    public static async Task<HttpResponseMessage> SendDeleteCommand(this HttpClient client, string relativeURL, object command) =>
        await client.SendCommand(HttpMethod.Delete, relativeURL, command);

    public static async Task<HttpResponseMessage> SendCommand(this HttpClient client, HttpMethod method, string relativeURL, object command)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));

        var request = new HttpRequestMessage(method, new Uri(relativeURL, UriKind.Relative))
        {
            Content = JsonContent.Create(command, options: CamelCase)
        };
        return await client.SendAsync(request);
    }
}
