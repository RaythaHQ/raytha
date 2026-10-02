using System.Text.Json;
using FluentAssertions;
using Raytha.Application.ContentTypes;

namespace Raytha.Application.UnitTests.ContentTypes;

public class ContentTypeDtoTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static ContentTypeFieldDto Field(string name) =>
        new() { Id = Guid.NewGuid(), DeveloperName = name };

    [Test]
    public void Serialized_WithFields_ListsThemAndThePrimaryField()
    {
        var title = Field("title");
        var type = new ContentTypeDto
        {
            DeveloperName = "posts",
            PrimaryFieldId = title.Id,
            ContentTypeFields = [title],
        };

        var json = JsonSerializer.SerializeToElement(type, Web);

        json.GetProperty("contentTypeFields").GetArrayLength().Should().Be(1);
        json.GetProperty("primaryField").GetProperty("developerName").GetString().Should().Be("title");
    }

    [Test]
    public void Serialized_WithoutFields_LeavesOutTheFieldModelButKeepsTheSummary()
    {
        var type = new ContentTypeDto
        {
            DeveloperName = "posts",
            LabelPlural = "Posts",
            PrimaryFieldId = Guid.NewGuid(),
            ContentTypeFields = null,
        };

        var json = JsonSerializer.SerializeToElement(type, Web);

        json.TryGetProperty("contentTypeFields", out _).Should().BeFalse();
        json.TryGetProperty("primaryField", out _).Should().BeFalse();
        json.GetProperty("developerName").GetString().Should().Be("posts");
        json.TryGetProperty("primaryFieldId", out _).Should().BeTrue();
    }

    [Test]
    public void LookupsOnACompactType_ReturnNothingInsteadOfThrowing()
    {
        var type = new ContentTypeDto { ContentTypeFields = null };

        type.PrimaryField.Should().BeNull();
        type.GetCustomField("title").Should().BeNull();
    }
}
