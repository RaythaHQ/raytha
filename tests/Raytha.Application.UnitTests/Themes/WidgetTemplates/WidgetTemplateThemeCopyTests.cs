using System.Text.Json;
using FluentAssertions;
using Raytha.Application.Themes;
using Raytha.Application.Themes.WidgetTemplates;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.UnitTests.Themes.WidgetTemplates;

public class WidgetTemplateThemeCopyTests
{
    private static readonly FieldDefinition[] CustomFields =
    [
        new() { DeveloperName = "tint", Label = "Tint", FieldType = "color", DefaultValue = JsonSerializer.SerializeToElement("#112233") },
        new()
        {
            DeveloperName = "points",
            Label = "Points",
            FieldType = "repeater",
            SubFields = [new() { DeveloperName = "text", Label = "Text", FieldType = "single_line_text", IsRequired = true }],
        },
    ];

    private static WidgetTemplate Custom() =>
        new()
        {
            Id = Guid.NewGuid(),
            ThemeId = Guid.NewGuid(),
            DeveloperName = "callout",
            Label = "Callout",
            Content = "<div></div>",
            Fields = CustomFields,
        };

    [Test]
    public void Duplicating_a_theme_copies_each_template_with_its_fields()
    {
        var original = Custom();
        var themeId = Guid.NewGuid();

        var copy = original.CopyToTheme(themeId);

        copy.Id.Should().NotBe(original.Id);
        copy.ThemeId.Should().Be(themeId);
        (copy.Label, copy.DeveloperName, copy.Content, copy.IsBuiltInTemplate, copy._FieldsJson)
            .Should()
            .Be((original.Label, original.DeveloperName, original.Content, original.IsBuiltInTemplate, original._FieldsJson));
    }

    [Test]
    public void An_exported_theme_imports_with_the_same_fields()
    {
        var hero = BuiltInWidgetType.Hero.CreateTemplate(Guid.NewGuid());
        hero.Fields = [.. hero.Fields.Take(2)];
        var exported = JsonSerializer.Serialize(
            new ThemeJson
            {
                WebTemplates = [],
                WidgetTemplates = [WidgetTemplateJson.GetProjection(hero), WidgetTemplateJson.GetProjection(Custom())],
                MediaItems = [],
            },
            new JsonSerializerOptions { WriteIndented = true }
        );

        var imported = JsonSerializer.Deserialize<ThemeJson>(exported)!.WidgetTemplates.Select(t => t.ToWidgetTemplate(Guid.NewGuid())).ToList();

        imported[0]._FieldsJson.Should().Be(hero._FieldsJson);
        imported[1]._FieldsJson.Should().Be(FieldDefinition.ListToJson(CustomFields));
    }

    [Test]
    public void A_theme_exported_before_fields_existed_gets_default_fields_for_built_ins_only()
    {
        const string exported = """
            {"WebTemplates":[],"MediaItems":[],"WidgetTemplates":[
              {"Label":"My hero","DeveloperName":"hero","Content":"<h1>{{ widget.settings.headline }}</h1>","IsBuiltInTemplate":true},
              {"Label":"Callout","DeveloperName":"callout","Content":"<div></div>","IsBuiltInTemplate":false}
            ]}
            """;

        var imported = JsonSerializer.Deserialize<ThemeJson>(exported)!.WidgetTemplates.Select(t => t.ToWidgetTemplate(Guid.NewGuid())).ToList();

        imported[0].Label.Should().Be("My hero");
        imported[0].Content.Should().Be("<h1>{{ widget.settings.headline }}</h1>");
        imported[0]._FieldsJson.Should().Be(FieldDefinition.ListToJson(BuiltInWidgetType.Hero.Fields));
        imported[1].Fields.Should().BeEmpty();
    }

    [Test]
    public void An_imported_template_with_invalid_fields_is_refused()
    {
        const string exported = """
            {"WebTemplates":[],"MediaItems":[],"WidgetTemplates":[
              {"Label":"Callout","DeveloperName":"callout","Content":"<div></div>","IsBuiltInTemplate":false,
               "Fields":[{"DeveloperName":"tint","Label":"Tint","FieldType":"color"},
                         {"DeveloperName":"tint","Label":"Tint again","FieldType":"marquee"}]}
            ]}
            """;
        var package = JsonSerializer.Deserialize<ThemeJson>(exported)!.WidgetTemplates.Single();

        var import = () => package.ToWidgetTemplate(Guid.NewGuid());

        import.Should().Throw<InvalidOperationException>().WithMessage("Widget template 'callout' has invalid fields.*");
    }

    [Test]
    public void Reverting_restores_the_fields_saved_in_the_revision()
    {
        var template = Custom();
        var revision = template.ToRevision();
        template.Fields = [CustomFields[0]];
        template.Label = "Changed";

        template.RestoreFrom(revision);

        template.Label.Should().Be("Callout");
        template._FieldsJson.Should().Be(FieldDefinition.ListToJson(CustomFields));
    }

    [Test]
    public void Reverting_to_a_revision_saved_before_fields_existed_keeps_the_current_fields()
    {
        var template = Custom();
        var revision = new WidgetTemplateRevision
        {
            Id = Guid.NewGuid(),
            WidgetTemplateId = template.Id,
            Label = "Old",
            Content = "<p>old</p>",
        };

        template.RestoreFrom(revision);

        template.Content.Should().Be("<p>old</p>");
        template._FieldsJson.Should().Be(FieldDefinition.ListToJson(CustomFields));
    }
}
