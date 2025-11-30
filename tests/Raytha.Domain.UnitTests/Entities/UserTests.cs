using FluentAssertions;
using Raytha.Domain.Entities;
using System.Text.Json;

namespace Raytha.Domain.UnitTests.Entities;

public class UserTests
{
    [Test]
    public void RecentlyAccessedViews_ShouldDeserialize_WhenBackingFieldIsSet()
    {
        // Arrange
        var entity = new User();
        var expectedViews = new List<RecentlyAccessedView>
        {
            new RecentlyAccessedView { ContentTypeId = Guid.NewGuid(), ViewId = Guid.NewGuid() }
        };
        entity._RecentlyAccessedViews = JsonSerializer.Serialize(expectedViews);

        // Act
        var result = entity.RecentlyAccessedViews;

        // Assert
        result.Should().HaveCount(1);
        result.First().ContentTypeId.Should().Be(expectedViews[0].ContentTypeId);
    }

    [Test]
    public void RecentlyAccessedViews_ShouldSerialize_WhenPropertyIsSet()
    {
        // Arrange
        var entity = new User();
        var views = new List<RecentlyAccessedView>
        {
            new RecentlyAccessedView { ContentTypeId = Guid.NewGuid(), ViewId = Guid.NewGuid() }
        };

        // Act
        entity.RecentlyAccessedViews = views;

        // Assert
        entity._RecentlyAccessedViews.Should().Contain(views[0].ContentTypeId.ToString());
    }

    [Test]
    public void RecentlyAccessedViews_ShouldDefaultToEmptyList_WhenBackingFieldIsNull()
    {
        // Arrange
        var entity = new User();
        entity._RecentlyAccessedViews = null;

        // Act
        var result = entity.RecentlyAccessedViews;

        // Assert
        result.Should().BeEmpty();
    }
}

