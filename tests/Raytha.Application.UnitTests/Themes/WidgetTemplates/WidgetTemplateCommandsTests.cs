using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.Themes.WidgetTemplates.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.UnitTests.Themes.WidgetTemplates;

public class WidgetTemplateCommandsTests
{
    private readonly Guid _themeId = Guid.NewGuid();
    private Mock<IRaythaDbContext> _db = null!;
    private List<WidgetTemplate> _templates = null!;
    private List<WidgetTemplateRevision> _revisions = null!;
    private List<SitePage> _pages = null!;
    private readonly ILiquidTemplateParser _liquid = Mock.Of<ILiquidTemplateParser>();

    private static readonly FieldDefinition[] CalloutFields =
    [
        new() { DeveloperName = "tint", Label = "Tint", FieldType = "color" },
        new()
        {
            DeveloperName = "points",
            Label = "Points",
            FieldType = "repeater",
            SubFields = [new() { DeveloperName = "text", Label = "Text", FieldType = "single_line_text" }],
        },
    ];

    [SetUp]
    public void Setup()
    {
        _db = new Mock<IRaythaDbContext>();
        _templates = [BuiltInWidgetType.Hero.CreateTemplate(_themeId)];
        _revisions = [];
        _pages = [];

        var templates = _templates.AsQueryable().BuildMockDbSet();
        templates.Setup(x => x.Add(It.IsAny<WidgetTemplate>())).Callback<WidgetTemplate>(_templates.Add);
        templates.Setup(x => x.Remove(It.IsAny<WidgetTemplate>())).Callback<WidgetTemplate>(t => _templates.Remove(t));
        var revisions = _revisions.AsQueryable().BuildMockDbSet();
        revisions.Setup(x => x.Add(It.IsAny<WidgetTemplateRevision>())).Callback<WidgetTemplateRevision>(_revisions.Add);

        _db.Setup(x => x.WidgetTemplates).Returns(templates.Object);
        _db.Setup(x => x.WidgetTemplateRevisions).Returns(revisions.Object);
        _db.Setup(x => x.SitePages).Returns(() => _pages.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Themes)
            .Returns(
                new List<Theme>
                {
                    new()
                    {
                        Id = _themeId,
                        Title = "Default",
                        DeveloperName = "default",
                        Description = "",
                    },
                }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object
            );
    }

    private CreateWidgetTemplate.Command Create(string developerName = "Callout", params FieldDefinition[] fields) =>
        new()
        {
            ThemeId = _themeId,
            Label = "Callout",
            DeveloperName = developerName,
            Content = "<div>{{ widget.settings.tint }}</div>",
            Fields = fields.Length == 0 ? CalloutFields : fields,
        };

    [Test]
    public async Task Create_stores_a_custom_template_with_its_fields()
    {
        var command = Create("My Callout");
        new CreateWidgetTemplate.Validator(_db.Object, _liquid).Validate(command).IsValid.Should().BeTrue();

        var result = await new CreateWidgetTemplate.Handler(_db.Object).Handle(command, CancellationToken.None);

        var created = _templates.Single(t => t.Id == result.Result.Guid);
        created.DeveloperName.Should().Be("my_callout");
        created.ThemeId.Should().Be(_themeId);
        created.IsBuiltInTemplate.Should().BeFalse();
        created.Fields.Should().BeEquivalentTo(CalloutFields);
    }

    [Test]
    public void Create_rejects_a_built_in_or_taken_developer_name()
    {
        Messages(new CreateWidgetTemplate.Validator(_db.Object, _liquid).Validate(Create("hero")))
            .Should()
            .Contain("That developer name is reserved for a built-in widget.");

        _templates.Add(new WidgetTemplate { ThemeId = _themeId, DeveloperName = "callout", Label = "Callout", Content = "x" });
        Messages(new CreateWidgetTemplate.Validator(_db.Object, _liquid).Validate(Create("Callout")))
            .Should()
            .Equal("A widget template with that developer name already exists in this theme.");
    }

    [Test]
    public void Create_requires_label_content_and_developer_name()
    {
        var command = Create() with { Label = "", Content = "", DeveloperName = "" };

        Messages(new CreateWidgetTemplate.Validator(_db.Object, _liquid).Validate(command))
            .Should()
            .BeEquivalentTo("Label is required.", "Content is required.", "Developer name is required.");
    }

    [Test]
    public void Create_rejects_liquid_that_does_not_parse()
    {
        var liquid = new Mock<ILiquidTemplateParser>();
        liquid
            .Setup(parser => parser.GetSyntaxError("{% if %}"))
            .Returns(LiquidSyntaxError.FromParserMessage("Invalid 'if' tag at (1:6)"));

        Messages(
                new CreateWidgetTemplate.Validator(_db.Object, liquid.Object).Validate(
                    Create() with { Content = "{% if %}" }
                )
            )
            .Should()
            .Contain(message => message.Contains("Line 1, column 6"));
    }

    [Test]
    public void Create_rejects_invalid_fields()
    {
        var command = Create("callout", new FieldDefinition { DeveloperName = "bad-name", Label = "Bad", FieldType = "color" });

        var result = new CreateWidgetTemplate.Validator(_db.Object, _liquid).Validate(command);

        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Fields");
    }

    [Test]
    public void Create_in_a_missing_theme_is_not_found()
    {
        var act = () => new CreateWidgetTemplate.Validator(_db.Object, _liquid).Validate(Create() with { ThemeId = Guid.NewGuid() });

        act.Should().Throw<NotFoundException>();
    }

    [Test]
    public async Task Edit_saves_a_revision_with_the_previous_fields_then_replaces_them()
    {
        var hero = _templates[0];
        var previousFields = hero._FieldsJson;
        var command = new EditWidgetTemplate.Command
        {
            Id = hero.Id,
            Label = "Banner",
            Content = "<section></section>",
            Fields = CalloutFields,
        };
        new EditWidgetTemplate.Validator(_db.Object, _liquid).Validate(command).IsValid.Should().BeTrue();

        await new EditWidgetTemplate.Handler(_db.Object).Handle(command, CancellationToken.None);

        hero.Label.Should().Be("Banner");
        hero.Fields.Should().BeEquivalentTo(CalloutFields);
        _revisions.Should().ContainSingle().Which._FieldsJson.Should().Be(previousFields);
    }

    [Test]
    public async Task Edit_without_fields_keeps_the_current_fields()
    {
        var hero = _templates[0];

        await new EditWidgetTemplate.Handler(_db.Object).Handle(
            new EditWidgetTemplate.Command { Id = hero.Id, Label = "Hero", Content = "x" },
            CancellationToken.None
        );

        hero._FieldsJson.Should().Be(FieldDefinition.ListToJson(BuiltInWidgetType.Hero.Fields));
    }

    [Test]
    public void Edit_rejects_invalid_fields()
    {
        var command = new EditWidgetTemplate.Command
        {
            Id = _templates[0].Id,
            Label = "Hero",
            Content = "x",
            Fields = [new FieldDefinition { DeveloperName = "size", Label = "Size", FieldType = "dropdown" }],
        };

        Messages(new EditWidgetTemplate.Validator(_db.Object, _liquid).Validate(command))
            .Should()
            .Equal("Field 'Size' needs at least one choice.");
    }

    [Test]
    public void Delete_refuses_a_built_in_template()
    {
        Messages(new DeleteWidgetTemplate.Validator(_db.Object).Validate(new DeleteWidgetTemplate.Command { Id = _templates[0].Id }))
            .Should()
            .Equal("Built-in widget templates cannot be deleted. Reset them to default instead.");
    }

    [Test]
    public void Delete_refuses_a_template_used_on_a_page_and_names_the_pages()
    {
        var callout = AddCallout();
        _pages.Add(PageUsing("Pricing", "callout", published: false));
        _pages.Add(PageUsing("About", "callout", published: true));
        _pages.Add(PageUsing("Home", "hero", published: true));

        Messages(new DeleteWidgetTemplate.Validator(_db.Object).Validate(new DeleteWidgetTemplate.Command { Id = callout.Id }))
            .Should()
            .Equal("This widget template is used on these site pages: About, Pricing. Remove those widgets before deleting it.");
    }

    [Test]
    public async Task Delete_removes_an_unused_custom_template()
    {
        var callout = AddCallout();
        _pages.Add(PageUsing("Home", "hero", published: true));
        var command = new DeleteWidgetTemplate.Command { Id = callout.Id };
        new DeleteWidgetTemplate.Validator(_db.Object).Validate(command).IsValid.Should().BeTrue();

        await new DeleteWidgetTemplate.Handler(_db.Object).Handle(command, CancellationToken.None);

        _templates.Should().NotContain(callout);
    }

    [Test]
    public void Delete_of_a_missing_template_is_not_found()
    {
        var act = () => new DeleteWidgetTemplate.Validator(_db.Object).Validate(new DeleteWidgetTemplate.Command { Id = Guid.NewGuid() });

        act.Should().Throw<NotFoundException>();
    }

    private WidgetTemplate AddCallout()
    {
        var callout = new WidgetTemplate
        {
            Id = Guid.NewGuid(),
            ThemeId = _themeId,
            DeveloperName = "callout",
            Label = "Callout",
            Content = "x",
            IsBuiltInTemplate = false,
            Fields = CalloutFields,
        };
        _templates.Add(callout);
        return callout;
    }

    private static SitePage PageUsing(string title, string widgetType, bool published)
    {
        var page = new SitePage { Id = Guid.NewGuid(), Title = title };
        var widgets = new Dictionary<string, List<SitePageWidget>>
        {
            ["main"] = [new SitePageWidget { Id = Guid.NewGuid(), WidgetType = widgetType }],
        };
        if (published)
            page.PublishedWidgets = widgets;
        else
            page.DraftWidgets = widgets;
        return page;
    }

    private static IEnumerable<string> Messages(FluentValidation.Results.ValidationResult result) =>
        result.Errors.Select(e => e.ErrorMessage);
}
