using FluentAssertions;
using Raytha.Domain.Entities;
using System.Text.Json;

namespace Raytha.Domain.UnitTests.Entities;

public class ContentItemRevisionTests
{
    [Test]
    public void PublishedContent_ShouldDeserialize_WhenBackingFieldIsSet()
    {
        // Arrange
        var entity = new ContentItemRevision();
        var expectedContent = new { title = "Test Title" };
        entity._PublishedContent = JsonSerializer.Serialize(expectedContent);

        // Act
        var result = entity.PublishedContent;

        // Assert
        ((string)result.GetProperty("title").ToString()).Should().Be("Test Title");
    }

    [Test]
    public void PublishedContent_ShouldSerialize_WhenPropertyIsSet()
    {
        // Arrange
        var entity = new ContentItemRevision();
        var content = new { title = "Test Title" };

        // Act
        entity.PublishedContent = content;

        // Assert
        entity._PublishedContent.Should().Contain("Test Title");
    }

    [Test]
    public void PublishedContent_ShouldDefaultToArray_WhenBackingFieldIsNull()
    {
        // Arrange
        var entity = new ContentItemRevision();
        entity._PublishedContent = null;

        // Act
        var result = entity.PublishedContent;

        // Assert
        // Current implementation defaults to "[]"
        ((string)result.ToString()).Should().Be("[]"); 
    }
}

