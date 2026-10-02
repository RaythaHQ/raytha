using CSharpVitamins;
using FluentAssertions;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentItems;
using Raytha.Application.Views;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentItems;

public class ContentItemListRenderModelTests
{
    [Test]
    public void An_item_without_a_template_for_the_active_theme_uses_the_built_in_detail_template()
    {
        var missing = new ContentItemDto
        {
            Id = ShortGuid.NewGuid(),
            PrimaryField = "Hello",
            RoutePath = "hello",
        };
        var boundId = ShortGuid.NewGuid();
        var bound = new ContentItemDto
        {
            Id = boundId,
            PrimaryField = "Bound",
            RoutePath = "bound",
        };
        var list = new ListResultDto<ContentItemDto>(new[] { missing, bound }, 2);
        var templates = new Dictionary<ShortGuid, string> { [boundId] = "custom_detail" };

        var model = ContentItemListResult_RenderModel.GetProjection(
            list,
            templates,
            new ViewDto { RoutePath = "posts", DeveloperName = "posts", Label = "Posts" }
        );

        model.Items.Select(item => item.Template).Should().Equal(
            BuiltInWebTemplate.ContentItemDetailViewPage.DeveloperName,
            "custom_detail"
        );
    }
}
