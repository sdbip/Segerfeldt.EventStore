using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.NUnit;

public static class HttpClientExtension
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<HttpResponseMessage> PostCommand(this HttpClient client, string relativeURL, object command) =>
        await client.PostAsJsonAsync(new Uri(relativeURL, UriKind.Relative), command, CamelCase);
}
