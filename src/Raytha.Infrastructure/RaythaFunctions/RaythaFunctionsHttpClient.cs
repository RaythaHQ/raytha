using Raytha.Application.Common.Interfaces;
using System.Text;
using System.Text.Json;

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

    public dynamic Post(string url, IDictionary<string, object> headers = null, IDictionary<string, object> body = null, bool json = true)
    {
        return MakeRequest(url, HttpMethod.Post, headers, body, json);
    }

    public dynamic Put(string url, IDictionary<string, object> headers = null, IDictionary<string, object> body = null, bool json = true)
    {
        return MakeRequest(url, HttpMethod.Put, headers, body, json);
    }

    public dynamic Delete(string url, IDictionary<string, object> headers = null)
    {
        return MakeRequest(url, HttpMethod.Delete, headers);
    }

    private dynamic MakeRequest(string url, HttpMethod method, IDictionary<string, object> headers = null, IDictionary<string, object> body = null, bool json = true)
    {
        if (headers != null)
        {
            foreach (var header in headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, (string)header.Value);
            }
        }

        var request = new HttpRequestMessage(method, url);

        if (body != null)
        {
            if (json)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            }
            else
            {
                IEnumerable<KeyValuePair<string, string>> bodyAsKv = body
                    .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.ToString()));
                request.Content = new FormUrlEncodedContent(bodyAsKv);
            }
        }

        var response = _httpClient.Send(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with status code {response.StatusCode}");
        }

        var content = response.Content.ReadAsStringAsync().Result;

        return content;
    }
}
