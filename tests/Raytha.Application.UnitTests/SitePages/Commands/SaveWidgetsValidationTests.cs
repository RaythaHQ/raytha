using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.SitePages.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.UnitTests.SitePages.Commands;

public class SaveWidgetsValidationTests
{
    private readonly Guid _activeThemeId = Guid.NewGuid();
    private Mock<IRaythaDbContext> _db = null!;
    private SitePage _page = null!;
    private SitePageWidget _legacyHero = null!;
    private SitePageWidget _orphan = null!;

    [SetUp]
    public void Setup()
    {
        _legacyHero = new SitePageWidget
        {
            Id = Guid.NewGuid(),
            WidgetType = "hero",
            SettingsJson = """{"headline":"Old","minHeight":"640px","backgroundColor":"navy","buttonStyle":null}""",
        };
        _orphan = new SitePageWidget
        {
            Id = Guid.NewGuid(),
            WidgetType = "retired_widget",
            SettingsJson = """{"anything":1}""",
        };
        _page = new SitePage { Id = Guid.NewGuid(), IsPublished = true };
        _page.PublishedWidgets = new() { ["main"] = [_legacyHero, _orphan] };

        var callout = new WidgetTemplate
        {
            Id = Guid.NewGuid(),
            ThemeId = _activeThemeId,
            DeveloperName = "callout",
            Label = "Callout",
            Content = "x",
            Fields =
            [
                new() { DeveloperName = "tint", Label = "Tint", FieldType = "color" },
                new()
                {
                    DeveloperName = "points",
                    Label = "Points",
                    FieldType = "repeater",
                    SubFields = [new() { DeveloperName = "text", Label = "Text", FieldType = "single_line_text", IsRequired = true }],
                },
            ],
        };
        var otherTheme = BuiltInWidgetType.Embed.CreateTemplate(Guid.NewGuid());

        _db = new Mock<IRaythaDbContext>();
        _db.Setup(x => x.SitePages).Returns(new List<SitePage> { _page }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.OrganizationSettings)
            .Returns(
                new List<Domain.Entities.OrganizationSettings> { new() { ActiveThemeId = _activeThemeId } }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object
            );
        _db.Setup(x => x.WidgetTemplates)
            .Returns(
                new List<WidgetTemplate> { BuiltInWidgetType.Hero.CreateTemplate(_activeThemeId), callout, otherTheme }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object
            );
    }

    private List<(string Property, string Message)> Validate(params SaveWidgets.WidgetInput[] widgets) =>
        new SaveWidgets.Validator(_db.Object)
            .Validate(new SaveWidgets.Command { Id = _page.Id, SectionName = "main", Widgets = widgets })
            .Errors.Select(e => (e.PropertyName, e.ErrorMessage))
            .ToList();

    private static SaveWidgets.WidgetInput Input(SitePageWidget widget, string? settingsJson = null) =>
        new()
        {
            Id = widget.Id,
            WidgetType = widget.WidgetType,
            SettingsJson = settingsJson ?? widget.SettingsJson,
        };

    [Test]
    public void Resaving_legacy_widgets_unchanged_is_accepted()
    {
        Validate(Input(_legacyHero), Input(_orphan)).Should().BeEmpty();
    }

    [Test]
    public void Legacy_values_survive_an_edit_of_another_field()
    {
        Validate(Input(_legacyHero, """{"headline":"New","minHeight":"640px","backgroundColor":"navy","buttonStyle":null}"""))
            .Should()
            .BeEmpty();
    }

    [Test]
    public void A_changed_value_is_checked_against_the_active_theme_fields()
    {
        Validate(Input(_legacyHero, """{"headline":"Old","minHeight":"720px","backgroundColor":"navy"}"""))
            .Should()
            .Equal(("Widgets[0].SettingsJson", "Minimum height must be a number."));
    }

    [Test]
    public void A_new_widget_is_fully_checked()
    {
        Validate(new SaveWidgets.WidgetInput { WidgetType = "hero", SettingsJson = """{"minHeight":"640px","alignment":"middle"}""" })
            .Select(e => e.Message)
            .Should()
            .BeEquivalentTo("Minimum height must be a number.", "Alignment must be one of: left, center, right.");
    }

    [Test]
    public void A_new_widget_must_use_a_template_of_the_active_theme()
    {
        Validate(new SaveWidgets.WidgetInput { WidgetType = "embed", SettingsJson = "{}" })
            .Should()
            .Equal(("Widgets[0].WidgetType", "Widget type 'embed' is not in the active theme."));
    }

    [Test]
    public void A_saved_widget_changed_to_another_type_is_treated_as_new()
    {
        Validate(new SaveWidgets.WidgetInput { Id = _orphan.Id, WidgetType = "hero", SettingsJson = """{"minHeight":"x"}""" })
            .Should()
            .Equal(("Widgets[0].SettingsJson", "Minimum height must be a number."));
    }

    [Test]
    public void Custom_template_fields_are_checked()
    {
        Validate(
                new SaveWidgets.WidgetInput { WidgetType = "callout", SettingsJson = """{"tint":"#aabbcc","points":[{"text":"One"}]}""" },
                new SaveWidgets.WidgetInput { WidgetType = "callout", SettingsJson = """{"tint":"blue","points":[{}]}""" }
            )
            .Should()
            .Equal(
                ("Widgets[1].SettingsJson", "Tint must be a color like #1e293b."),
                ("Widgets[1].SettingsJson", "Points row 1: Text is required.")
            );
    }

    [Test]
    public void Invalid_json_on_a_widget_outside_the_theme_is_still_rejected()
    {
        Validate(Input(_orphan, "{not json"))
            .Should()
            .Equal(("Widgets[0].SettingsJson", "Invalid JSON format."));
    }

    [Test]
    public async Task Settings_are_stored_verbatim_with_unknown_keys_and_json_types()
    {
        const string settings = """{"headline":"Hi","minHeight":500,"customFlag":true,"extra":{"n":[1,"2"]}}""";
        var input = new SaveWidgets.WidgetInput { Id = _legacyHero.Id, WidgetType = "hero", SettingsJson = settings };
        Validate(input).Should().BeEmpty();

        await new SaveWidgets.Handler(_db.Object).Handle(
            new SaveWidgets.Command { Id = _page.Id, SectionName = "main", Widgets = [input] },
            CancellationToken.None
        );

        _page.DraftWidgets["main"].Single().SettingsJson.Should().Be(settings);
    }

    [Test]
    public void Edit_widget_checks_changed_values_only()
    {
        var validator = new EditWidget.Validator(_db.Object);
        EditWidget.Command Edit(string json) =>
            new() { SitePageId = _page.Id, SectionName = "main", WidgetId = _legacyHero.Id, SettingsJson = json };

        validator.Validate(Edit(_legacyHero.SettingsJson)).IsValid.Should().BeTrue();
        validator
            .Validate(Edit("""{"headline":"Old","minHeight":"640px","backgroundColor":"red"}"""))
            .Errors.Select(e => e.ErrorMessage)
            .Should()
            .Equal("Background color must be a color like #1e293b.");
    }
}
