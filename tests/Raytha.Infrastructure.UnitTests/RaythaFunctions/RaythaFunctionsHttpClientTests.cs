using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Raytha.Infrastructure.RaythaFunctions;

namespace Raytha.Infrastructure.UnitTests.RaythaFunctions;

[TestFixture]
public class RaythaFunctionsHttpClientTests
{
    #region Test Infrastructure

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestContent { get; private set; }
        public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseContent { get; set; } = "{}";
        public bool ThrowException { get; set; } = false;
        public Exception? ExceptionToThrow { get; set; }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (request.Content != null)
            {
                using var reader = new StreamReader(request.Content.ReadAsStream());
                LastRequestContent = reader.ReadToEnd();
            }

            if (ThrowException)
            {
                throw ExceptionToThrow ?? new Exception("Mock exception");
            }

            return new HttpResponseMessage(ResponseStatusCode)
            {
                Content = new StringContent(ResponseContent, Encoding.UTF8, "application/json")
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Send(request, cancellationToken));
        }
    }

    private MockHttpMessageHandler _mockHandler = null!;
    private HttpClient _httpClient = null!;
    private RaythaFunctionsHttpClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
        _client = new RaythaFunctionsHttpClient(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    #endregion

    #region Get Tests

    [Test]
    public void Get_ShouldSendGetRequest()
    {
        // Arrange
        _mockHandler.ResponseContent = "{\"result\": \"success\"}";

        // Act
        var result = _client.Get("https://api.example.com/data");

        // Assert
        _mockHandler.LastRequest.Should().NotBeNull();
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        _mockHandler.LastRequest.RequestUri!.ToString().Should().Be("https://api.example.com/data");
    }

    [Test]
    public void Get_ShouldReturnResponseContent()
    {
        // Arrange
        _mockHandler.ResponseContent = "{\"data\": \"test value\"}";

        // Act
        var result = _client.Get("https://api.example.com/data");

        // Assert
        ((string)result).Should().Be("{\"data\": \"test value\"}");
    }

    [Test]
    public void Get_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var headers = new Dictionary<string, object>
        {
            { "Authorization", "Bearer token123" },
            { "X-Custom-Header", "custom-value" }
        };

        // Act
        _client.Get("https://api.example.com/data", headers);

        // Assert
        _mockHandler.LastRequest!.Headers.Should().Contain(h =>
            h.Key == "Authorization" && h.Value.First() == "Bearer token123");
        _mockHandler.LastRequest.Headers.Should().Contain(h =>
            h.Key == "X-Custom-Header" && h.Value.First() == "custom-value");
    }

    [Test]
    public void Get_WithNullHeaders_ShouldNotThrow()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";

        // Act
        var action = () => _client.Get("https://api.example.com/data", null);

        // Assert
        action.Should().NotThrow();
    }

    [Test]
    public void Get_WhenRequestFails_ShouldThrowException()
    {
        // Arrange
        _mockHandler.ResponseStatusCode = HttpStatusCode.NotFound;

        // Act & Assert
        var action = () => _client.Get("https://api.example.com/data");
        action.Should().Throw<Exception>()
            .WithMessage("*NotFound*");
    }

    #endregion

    #region Post Tests

    [Test]
    public void Post_ShouldSendPostRequest()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";

        // Act
        _client.Post("https://api.example.com/data");

        // Assert
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Post);
    }

    [Test]
    public void Post_WithJsonBody_ShouldSerializeToJson()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var body = new Dictionary<string, object>
        {
            { "name", "John" },
            { "age", 30 }
        };

        // Act
        _client.Post("https://api.example.com/data", body: body, json: true);

        // Assert
        _mockHandler.LastRequest!.Content.Should().NotBeNull();
        _mockHandler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        _mockHandler.LastRequestContent.Should().Contain("John");
        _mockHandler.LastRequestContent.Should().Contain("30");
    }

    [Test]
    public void Post_WithFormBody_ShouldSendFormUrlEncoded()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var body = new Dictionary<string, object>
        {
            { "username", "testuser" },
            { "password", "testpass" }
        };

        // Act
        _client.Post("https://api.example.com/login", body: body, json: false);

        // Assert
        _mockHandler.LastRequest!.Content.Should().BeOfType<FormUrlEncodedContent>();
        _mockHandler.LastRequestContent.Should().Contain("username=testuser");
        _mockHandler.LastRequestContent.Should().Contain("password=testpass");
    }

    [Test]
    public void Post_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var headers = new Dictionary<string, object>
        {
            { "Content-Type", "application/json" },
            { "X-API-Key", "secret-key" }
        };

        // Act
        _client.Post("https://api.example.com/data", headers);

        // Assert
        _mockHandler.LastRequest!.Headers.Should().Contain(h =>
            h.Key == "X-API-Key" && h.Value.First() == "secret-key");
    }

    [Test]
    public void Post_WithNullBodyValue_ShouldHandleGracefully()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var body = new Dictionary<string, object>
        {
            { "name", "John" },
            { "nullField", null! }
        };

        // Act
        var action = () => _client.Post("https://api.example.com/data", body: body, json: true);

        // Assert
        action.Should().NotThrow();
    }

    [Test]
    public void Post_WhenRequestFails_ShouldThrowException()
    {
        // Arrange
        _mockHandler.ResponseStatusCode = HttpStatusCode.InternalServerError;

        // Act & Assert
        var action = () => _client.Post("https://api.example.com/data");
        action.Should().Throw<Exception>()
            .WithMessage("*InternalServerError*");
    }

    #endregion

    #region Put Tests

    [Test]
    public void Put_ShouldSendPutRequest()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";

        // Act
        _client.Put("https://api.example.com/data/1");

        // Assert
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Put);
    }

    [Test]
    public void Put_WithJsonBody_ShouldSerializeToJson()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var body = new Dictionary<string, object>
        {
            { "id", 1 },
            { "name", "Updated" }
        };

        // Act
        _client.Put("https://api.example.com/data/1", body: body, json: true);

        // Assert
        _mockHandler.LastRequest!.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        _mockHandler.LastRequestContent.Should().Contain("Updated");
    }

    [Test]
    public void Put_WithFormBody_ShouldSendFormUrlEncoded()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var body = new Dictionary<string, object>
        {
            { "field", "value" }
        };

        // Act
        _client.Put("https://api.example.com/data/1", body: body, json: false);

        // Assert
        _mockHandler.LastRequest!.Content.Should().BeOfType<FormUrlEncodedContent>();
    }

    [Test]
    public void Put_WhenRequestFails_ShouldThrowException()
    {
        // Arrange
        _mockHandler.ResponseStatusCode = HttpStatusCode.Forbidden;

        // Act & Assert
        var action = () => _client.Put("https://api.example.com/data/1");
        action.Should().Throw<Exception>()
            .WithMessage("*Forbidden*");
    }

    #endregion

    #region Delete Tests

    [Test]
    public void Delete_ShouldSendDeleteRequest()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";

        // Act
        _client.Delete("https://api.example.com/data/1");

        // Assert
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
    }

    [Test]
    public void Delete_WithHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var headers = new Dictionary<string, object>
        {
            { "Authorization", "Bearer delete-token" }
        };

        // Act
        _client.Delete("https://api.example.com/data/1", headers);

        // Assert
        _mockHandler.LastRequest!.Headers.Should().Contain(h =>
            h.Key == "Authorization" && h.Value.First() == "Bearer delete-token");
    }

    [Test]
    public void Delete_WhenRequestFails_ShouldThrowException()
    {
        // Arrange
        _mockHandler.ResponseStatusCode = HttpStatusCode.Unauthorized;

        // Act & Assert
        var action = () => _client.Delete("https://api.example.com/data/1");
        action.Should().Throw<Exception>()
            .WithMessage("*Unauthorized*");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    [Parallelizable(ParallelScope.All)]
    public void Request_WithErrorStatusCode_ShouldThrowException(HttpStatusCode statusCode)
    {
        // Arrange
        var handler = new MockHttpMessageHandler { ResponseStatusCode = statusCode };
        using var client = new HttpClient(handler);
        var raythaClient = new RaythaFunctionsHttpClient(client);

        // Act & Assert
        var action = () => raythaClient.Get("https://api.example.com/data");
        action.Should().Throw<Exception>()
            .WithMessage($"*{statusCode}*");
    }

    #endregion

    #region Response Handling Tests

    [Test]
    public void Request_ShouldReturnPlainTextResponse()
    {
        // Arrange
        _mockHandler.ResponseContent = "Plain text response";

        // Act
        var result = _client.Get("https://api.example.com/text");

        // Assert
        ((string)result).Should().Be("Plain text response");
    }

    [Test]
    public void Request_ShouldReturnJsonResponse()
    {
        // Arrange
        var responseObject = new { status = "ok", count = 42 };
        _mockHandler.ResponseContent = JsonSerializer.Serialize(responseObject);

        // Act
        var result = _client.Get("https://api.example.com/json");

        // Assert
        ((string)result).Should().Contain("status");
        ((string)result).Should().Contain("ok");
        ((string)result).Should().Contain("42");
    }

    [Test]
    public void Request_WithEmptyResponse_ShouldReturnEmptyString()
    {
        // Arrange
        _mockHandler.ResponseContent = "";

        // Act
        var result = _client.Get("https://api.example.com/empty");

        // Assert
        ((string)result).Should().BeEmpty();
    }

    #endregion

    #region Header Tests

    [Test]
    public void Request_WithMultipleHeaders_ShouldIncludeAll()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var headers = new Dictionary<string, object>
        {
            { "X-Header-1", "Value1" },
            { "X-Header-2", "Value2" },
            { "X-Header-3", "Value3" }
        };

        // Act
        _client.Get("https://api.example.com/data", headers);

        // Assert
        _mockHandler.LastRequest!.Headers.Count().Should().BeGreaterThanOrEqualTo(3);
    }

    [Test]
    public void Request_WithNullHeaderValue_ShouldHandleGracefully()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var headers = new Dictionary<string, object>
        {
            { "X-Nullable-Header", null! }
        };

        // Act
        var action = () => _client.Get("https://api.example.com/data", headers);

        // Assert
        action.Should().NotThrow();
    }

    #endregion

    #region Body Serialization Tests

    [Test]
    public void Post_WithNestedObject_ShouldSerializeCorrectly()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";
        var body = new Dictionary<string, object>
        {
            { "user", new Dictionary<string, object> { { "name", "John" }, { "email", "john@example.com" } } },
            { "tags", new[] { "tag1", "tag2" } }
        };

        // Act
        _client.Post("https://api.example.com/data", body: body, json: true);

        // Assert
        _mockHandler.LastRequestContent.Should().Contain("user");
        _mockHandler.LastRequestContent.Should().Contain("name");
        _mockHandler.LastRequestContent.Should().Contain("John");
        _mockHandler.LastRequestContent.Should().Contain("tags");
    }

    [Test]
    public void Post_WithEmptyBody_ShouldNotSendContent()
    {
        // Arrange
        _mockHandler.ResponseContent = "{}";

        // Act
        _client.Post("https://api.example.com/data");

        // Assert
        _mockHandler.LastRequest!.Content.Should().BeNull();
    }

    #endregion
}

