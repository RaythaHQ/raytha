using FluentAssertions;
using Raytha.Application.SitePages.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.SitePages.Queries;

public class GetSitePageByIdTests
{
    [Test]
    public void TemplateSections_WalksParentTemplates_InFirstSeenOrder()
    {
        var layout = new WebTemplate
        {
            Id = Guid.NewGuid(),
            ThemeId = Guid.NewGuid(),
            Content = "{{ render_section(\"header\") }}{% renderbody %}{{ render_section(\"main\") }}",
        };
        var page = new WebTemplate
        {
            Id = Guid.NewGuid(),
            ThemeId = Guid.NewGuid(),
            Content = "{{ render_section('main') }}{% for w in get_section(\"sidebar\") %}{% endfor %}",
            ParentTemplate = layout,
        };

        GetSitePageById.Handler.TemplateSections(page)
            .Should()
            .Equal("main", "sidebar", "header");
    }

    [Test]
    public void TemplateSections_DefaultsToMain_WhenTemplateDeclaresNone()
    {
        var template = new WebTemplate { Id = Guid.NewGuid(), ThemeId = Guid.NewGuid(), Content = "<p>static</p>" };

        GetSitePageById.Handler.TemplateSections(template).Should().Equal("main");
        GetSitePageById.Handler.TemplateSections(null).Should().Equal("main");
    }

    [Test]
    public void TemplateSections_IgnoresSectionsInComments()
    {
        var template = new WebTemplate
        {
            Id = Guid.NewGuid(),
            ThemeId = Guid.NewGuid(),
            Content = "{% comment %}{{ render_section(\"old\") }}{% endcomment %}{{ render_section(\"main\") }}",
        };

        GetSitePageById.Handler.TemplateSections(template).Should().Equal("main");
    }
}
