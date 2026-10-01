using CSharpVitamins;
using FluentAssertions;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems;
using Raytha.Web.Services;

namespace Raytha.Architecture.Tests;

public class LiquidIdComparisonTests
{
    [Test]
    public void A_related_item_id_equals_the_target_id_in_liquid()
    {
        var id = ShortGuid.NewGuid();
        var target = new ContentItem_RenderModel { Id = id.ToString() };
        var related = new ContentItemDto { Id = id };

        Render("{% if related.Id == Target.Id %}same{% else %}different{% endif %}", target, related)
            .Should()
            .Be("same");
    }

    [Test]
    public void A_different_related_item_id_does_not_equal_the_target_id()
    {
        var target = new ContentItem_RenderModel { Id = ShortGuid.NewGuid().ToString() };
        var related = new ContentItemDto { Id = ShortGuid.NewGuid() };

        Render("{% if related.Id == Target.Id %}same{% else %}different{% endif %}", target, related)
            .Should()
            .Be("different");
    }

    private static string Render(string template, ContentItem_RenderModel target, ContentItemDto related)
    {
        var organization = new Mock<ICurrentOrganization>();
        organization.SetupGet(o => o.TimeZone).Returns("UTC");
        var engine = new RenderEngine(null!, null!, organization.Object, null!, null!, null!);
        return engine.RenderAsHtml(template, new { Target = target, related });
    }
}
