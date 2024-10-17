using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Projection.NUnit;

public static class HttpClientExtension
{
    /// <summary>Send a GET request with a serialized command to the application</summary>
    /// <param name="client">The client to send the command with</param>
    /// <param name="path">The URL path to query (e.g. "/Entity/id/property"</param>
    /// <param name="addHeaders">Action for configuring the request headers</param>
    /// <returns>The response from the command handler (or 404 NOT FOUND if the command handler is not set up correctly)</returns>
    public static async Task<HttpResponseMessage> SendGet(this HttpClient client, string path, Action<HttpRequestHeaders>? addHeaders = null)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        addHeaders?.Invoke(request.Headers);
        return await client.SendAsync(request);
    }
}
