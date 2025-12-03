using System.Text.Json;
using FluentAssertions;
using Raytha.Infrastructure.RaythaFunctions;

namespace Raytha.Infrastructure.UnitTests.RaythaFunctions;

[TestFixture]
public class RaythaFunctionResultTests
{
    #region Property Tests

    [Test]
    public void ContentType_ShouldBeSettableAndGettable()
    {
        // Arrange
        var result = new RaythaFunctionResult();

        // Act
        result.contentType = "application/json";

        // Assert
        result.contentType.Should().Be("application/json");
    }

    [Test]
    public void Body_ShouldAcceptStringValue()
    {
        // Arrange
        var result = new RaythaFunctionResult();

        // Act
        result.body = "Hello, World!";

        // Assert
        result.body.Should().Be("Hello, World!");
    }

    [Test]
    public void Body_ShouldAcceptObjectValue()
    {
        // Arrange
        var result = new RaythaFunctionResult();
        var bodyObject = new { name = "test", value = 42 };

        // Act
        result.body = bodyObject;

        // Assert
        result.body.Should().BeEquivalentTo(bodyObject);
    }

    [Test]
    public void Body_ShouldAcceptJsonElement()
    {
        // Arrange
        var result = new RaythaFunctionResult();
        var json = JsonSerializer.Deserialize<JsonElement>("{\"key\": \"value\"}");

        // Act
        result.body = json;

        // Assert
        result.body.Should().Be(json);
    }

    [Test]
    public void Body_ShouldAcceptNullValue()
    {
        // Arrange
        var result = new RaythaFunctionResult();

        // Act
        result.body = null;

        // Assert
        result.body.Should().BeNull();
    }

    [Test]
    public void StatusCode_ShouldBeSettableAndGettable()
    {
        // Arrange
        var result = new RaythaFunctionResult();

        // Act
        result.statusCode = 200;

        // Assert
        result.statusCode.Should().Be(200);
    }

    [Test]
    public void StatusCode_ShouldDefaultToZero()
    {
        // Arrange & Act
        var result = new RaythaFunctionResult();

        // Assert
        result.statusCode.Should().Be(0);
    }

    #endregion

    #region Common Content Type Tests

    [Test]
    [TestCase("application/json")]
    [TestCase("text/html")]
    [TestCase("application/xml")]
    [TestCase("redirectToUrl")]
    [TestCase("statusCode")]
    [Parallelizable(ParallelScope.All)]
    public void ContentType_ShouldAcceptVariousContentTypes(string contentType)
    {
        // Arrange
        var result = new RaythaFunctionResult();

        // Act
        result.contentType = contentType;

        // Assert
        result.contentType.Should().Be(contentType);
    }

    #endregion

    #region HTTP Status Code Tests

    [Test]
    [TestCase(200)]
    [TestCase(201)]
    [TestCase(204)]
    [TestCase(301)]
    [TestCase(302)]
    [TestCase(400)]
    [TestCase(401)]
    [TestCase(403)]
    [TestCase(404)]
    [TestCase(500)]
    [Parallelizable(ParallelScope.All)]
    public void StatusCode_ShouldAcceptCommonHttpStatusCodes(int statusCode)
    {
        // Arrange
        var result = new RaythaFunctionResult();

        // Act
        result.statusCode = statusCode;

        // Assert
        result.statusCode.Should().Be(statusCode);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void Result_ShouldRepresentJsonResult()
    {
        // Arrange & Act
        var result = new RaythaFunctionResult
        {
            contentType = "application/json",
            body = new { message = "Success", count = 5 },
            statusCode = 200
        };

        // Assert
        result.contentType.Should().Be("application/json");
        result.statusCode.Should().Be(200);
        result.body.Should().NotBeNull();
    }

    [Test]
    public void Result_ShouldRepresentHtmlResult()
    {
        // Arrange & Act
        var result = new RaythaFunctionResult
        {
            contentType = "text/html",
            body = "<html><body><h1>Hello</h1></body></html>",
            statusCode = 200
        };

        // Assert
        result.contentType.Should().Be("text/html");
        result.body.Should().Be("<html><body><h1>Hello</h1></body></html>");
    }

    [Test]
    public void Result_ShouldRepresentRedirectResult()
    {
        // Arrange & Act
        var result = new RaythaFunctionResult
        {
            contentType = "redirectToUrl",
            body = "/new-page",
            statusCode = 302
        };

        // Assert
        result.contentType.Should().Be("redirectToUrl");
        result.body.Should().Be("/new-page");
        result.statusCode.Should().Be(302);
    }

    [Test]
    public void Result_ShouldRepresentErrorResult()
    {
        // Arrange & Act
        var result = new RaythaFunctionResult
        {
            contentType = "statusCode",
            body = "Resource not found",
            statusCode = 404
        };

        // Assert
        result.contentType.Should().Be("statusCode");
        result.body.Should().Be("Resource not found");
        result.statusCode.Should().Be(404);
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void Result_ShouldSerializeToJson()
    {
        // Arrange
        var result = new RaythaFunctionResult
        {
            contentType = "application/json",
            body = "test body",
            statusCode = 200
        };

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<RaythaFunctionResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.contentType.Should().Be("application/json");
        deserialized.statusCode.Should().Be(200);
    }

    [Test]
    public void Result_WithComplexBody_ShouldSerializeCorrectly()
    {
        // Arrange
        var complexBody = new Dictionary<string, object>
        {
            { "items", new[] { 1, 2, 3 } },
            { "metadata", new { total = 3, page = 1 } }
        };

        var result = new RaythaFunctionResult
        {
            contentType = "application/json",
            body = complexBody,
            statusCode = 200
        };

        // Act
        var json = JsonSerializer.Serialize(result);

        // Assert
        json.Should().Contain("items");
        json.Should().Contain("metadata");
        json.Should().Contain("application/json");
    }

    #endregion
}

