using System.Text;
using System.Text.Json;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.RaythaFunctions;

public class RaythaFunctionsHttpClient : IRaythaFunctionsHttpClient
{
    private readonly HttpClient _httpClient;

    public RaythaFunctionsHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public dynamic Get(string url, IDictionary<string, object> headers = null)
    {
        return MakeRequest(url, HttpMethod.Get, headers);
    }

    public dynamic Post(
        string url,
        IDictionary<string, object> headers = null,
        IDictionary<string, object> body = null,
        bool json = true
    )
    {
        return MakeRequest(url, HttpMethod.Post, headers, body, json);
    }

    public dynamic Put(
        string url,
        IDictionary<string, object> headers = null,
        IDictionary<string, object> body = null,
        bool json = true
    )
    {
        return MakeRequest(url, HttpMethod.Put, headers, body, json);
    }

    public dynamic Delete(string url, IDictionary<string, object> headers = null)
    {
        return MakeRequest(url, HttpMethod.Delete, headers);
    }

    private dynamic MakeRequest(
        string url,
        HttpMethod method,
        IDictionary<string, object> headers = null,
        IDictionary<string, object> body = null,
        bool json = true
    )
    {
        using var request = new HttpRequestMessage(method, url);

        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value?.ToString());
            }
        }

        if (body != null)
        {
            if (json)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                );
            }
            else
            {
                IEnumerable<KeyValuePair<string, string>> bodyAsKv = body.Select(
                    kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString() ?? string.Empty)
                );
                request.Content = new FormUrlEncodedContent(bodyAsKv);
            }
        }

        // Use synchronous Send to avoid deadlocks in the V8 script engine context
        var response = _httpClient.Send(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with status code {response.StatusCode}");
        }

        // Read content synchronously
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }
}
