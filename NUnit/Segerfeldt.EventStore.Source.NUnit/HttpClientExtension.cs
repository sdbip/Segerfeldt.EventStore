using Microsoft.AspNetCore.Http.Extensions;

using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Segerfeldt.EventStore.Source.NUnit;

public static class HttpClientExtension
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Send a POST request with a serialized command to the application</summary>
    /// <param name="client">The client to send the command with</param>
    /// <param name="path">The URL path to query (e.g. "/Entity/id/property"</param>
    /// <param name="command">The command DTO to send</param>
    /// <param name="addHeaders">Action for configuring the request headers</param>
    /// <returns>The response from the command handler (or 404 NOT FOUND if the command handler is not set up correctly)</returns>
    public static async Task<HttpResponseMessage> SendPostCommand(this HttpClient client, string path, object command, Action<HttpRequestHeaders>? addHeaders = null) =>
        await client.SendCommand(HttpMethod.Post, path, command, addHeaders);

    /// <summary>Send a DELETE request with a serialized command to the application</summary>
    /// <param name="client">The client to send the command with</param>
    /// <param name="path">The URL path to query (e.g. "/Entity/id/property"</param>
    /// <param name="command">The command DTO to send</param>
    /// <param name="addHeaders">Action for configuring the request headers</param>
    /// <returns>The response from the command handler (or 404 NOT FOUND if the command handler is not set up correctly)</returns>
    public static async Task<HttpResponseMessage> SendDeleteCommand(this HttpClient client, string path, object command, Action<HttpRequestHeaders>? addHeaders = null) =>
        await client.SendCommandOnURL(HttpMethod.Delete, path, command, addHeaders);

    /// <summary>Send a DELETE request with a serialized command to the application</summary>
    /// <param name="client">The client to send the command with</param>
    /// <param name="method">The method/verb to use in the request</param>
    /// <param name="path">The URL path to query (e.g. "/Entity/id/property"</param>
    /// <param name="command">The command DTO to send</param>
    /// <param name="addHeaders">Action for configuring the request headers</param>
    /// <returns>The response from the command handler (or 404 NOT FOUND if the command handler is not set up correctly)</returns>
    public static async Task<HttpResponseMessage> SendCommandOnURL(this HttpClient client, HttpMethod method, string path, object command, Action<HttpRequestHeaders>? addHeaders = null)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));

        var queryBuilder = new QueryBuilder();
        foreach (var prop in command.GetType().GetProperties())
        {
            var value = prop.GetValue(command);
            if (value is string s)
                queryBuilder.Add(prop.Name, s);
            else if (value is IEnumerable a)
                queryBuilder.Add(prop.Name, a.Cast<object>().Select(x => $"{x}"));
            else if (value is not null)
                queryBuilder.Add(prop.Name, $"{value}");
        }
        var requestUri = new UriBuilder
        {
            Path = path,
            Query = queryBuilder.ToQueryString().ToUriComponent(),
        };
        var request = new HttpRequestMessage(method, requestUri.Uri);
        addHeaders?.Invoke(request.Headers);
        return await client.SendAsync(request);
    }

    /// <summary>Send a request with a serialized command to the application</summary>
    /// <param name="client">The client to send the command with</param>
    /// <param name="method">The method/verb to use in the request</param>
    /// <param name="path">The URL path to query (e.g. "/Entity/id/property"</param>
    /// <param name="command">The command DTO to send</param>
    /// <param name="addHeaders">Action for configuring the request headers</param>
    /// <returns>The response from the command handler (or 404 NOT FOUND if the command handler is not set up correctly)</returns>
    public static async Task<HttpResponseMessage> SendCommand(this HttpClient client, HttpMethod method, string path, object command, Action<HttpRequestHeaders>? addHeaders = null)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));

        var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative))
        {
            Content = JsonContent.Create(command, options: CamelCase)
        };
        addHeaders?.Invoke(request.Headers);
        return await client.SendAsync(request);
    }
}
