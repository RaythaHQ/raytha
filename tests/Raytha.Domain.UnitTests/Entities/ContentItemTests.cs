using FluentAssertions;
using Raytha.Domain.Entities;
using System.Text.Json;

namespace Raytha.Domain.UnitTests.Entities;

public class ContentItemTests
{
    [Test]
    public void DraftContent_ShouldDeserialize_WhenBackingFieldIsSet()
    {
        // Arrange
        var entity = new ContentItem();
        var expectedContent = new { title = "Test Title" };
        entity._DraftContent = JsonSerializer.Serialize(expectedContent);

        // Act
        var result = entity.DraftContent;

        // Assert
        ((string)result.GetProperty("title").ToString()).Should().Be("Test Title");
    }

    [Test]
    public void DraftContent_ShouldSerialize_WhenPropertyIsSet()
    {
        // Arrange
        var entity = new ContentItem();
        var content = new { title = "Test Title" };

        // Act
        entity.DraftContent = content;

        // Assert
        entity._DraftContent.Should().Contain("Test Title");
    }

    [Test]
    public void DraftContent_ShouldDefaultToEmptyObject_WhenBackingFieldIsNull()
    {
        // Arrange
        var entity = new ContentItem();
        entity._DraftContent = null;

        // Act
        var result = entity.DraftContent;

        // Assert
        ((string)result.ToString()).Should().Be("{}");
    }

    [Test]
    public void PublishedContent_ShouldDeserialize_WhenBackingFieldIsSet()
    {
        // Arrange
        var entity = new ContentItem();
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
        var entity = new ContentItem();
        var content = new { title = "Test Title" };

        // Act
        entity.PublishedContent = content;

        // Assert
        entity._PublishedContent.Should().Contain("Test Title");
    }

    [Test]
    public void PublishedContent_ShouldDefaultToEmptyObject_WhenBackingFieldIsNull()
    {
        // Arrange
        var entity = new ContentItem();
        entity._PublishedContent = null;

        // Act
        var result = entity.PublishedContent;

        // Assert
        ((string)result.ToString()).Should().Be("{}");
    }
}

