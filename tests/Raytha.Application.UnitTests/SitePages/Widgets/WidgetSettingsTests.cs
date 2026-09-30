using System.Text.Json;
using FluentAssertions;
using Raytha.Application.SitePages;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.UnitTests.SitePages.Widgets;

public class WidgetSettingsTests
{
    private static readonly IReadOnlyList<FieldDefinition> Fields =
    [
        new() { DeveloperName = "headline", Label = "Headline", FieldType = "single_line_text", IsRequired = true },
        new() { DeveloperName = "minHeight", Label = "Min height", FieldType = "number" },
        new() { DeveloperName = "expand", Label = "Expand", FieldType = "checkbox" },
        new() { DeveloperName = "color", Label = "Color", FieldType = "color" },
        new() { DeveloperName = "publishedOn", Label = "Published on", FieldType = "date" },
        new()
        {
            DeveloperName = "style",
            Label = "Style",
            FieldType = "dropdown",
            Choices =
            [
                new ContentTypeFieldChoice { DeveloperName = "light", Label = "Light" },
                new ContentTypeFieldChoice { DeveloperName = "dark", Label = "Dark" },
                new ContentTypeFieldChoice { DeveloperName = "retired", Label = "Retired", Disabled = true },
            ],
        },
        new()
        {
            DeveloperName = "items",
            Label = "Items",
            FieldType = "repeater",
            SubFields =
            [
                new() { DeveloperName = "question", Label = "Question", FieldType = "single_line_text", IsRequired = true },
                new() { DeveloperName = "votes", Label = "Votes", FieldType = "number" },
            ],
        },
    ];

    private static List<string> New(string json) => WidgetSettings.Validate(json, Fields, []).ToList();

    [Test]
    public void Valid_settings_of_every_type_pass()
    {
        New(
                """
                {"headline":"Hi","minHeight":400,"expand":false,"color":"#1E293b","publishedOn":"2026-09-29",
                 "style":"dark","items":[{"question":"Why?","votes":3}]}
                """
            )
            .Should()
            .BeEmpty();
    }

    [Test]
    public void A_required_field_that_is_missing_null_or_empty_fails()
    {
        New("""{}""").Should().Equal("Headline is required.");
        New("""{"headline":null}""").Should().Equal("Headline is required.");
        New("""{"headline":""}""").Should().Equal("Headline is required.");
    }

    [Test]
    public void Optional_fields_accept_missing_null_and_empty_values()
    {
        New("""{"headline":"Hi","minHeight":null,"color":"","style":"","items":[]}""").Should().BeEmpty();
    }

    [TestCase("""{"headline":"Hi","minHeight":"400"}""", "Min height must be a number.")]
    [TestCase("""{"headline":"Hi","expand":"true"}""", "Expand must be true or false.")]
    [TestCase("""{"headline":"Hi","color":"red"}""", "Color must be a color like #1e293b.")]
    [TestCase("""{"headline":"Hi","color":"#fff"}""", "Color must be a color like #1e293b.")]
    [TestCase("""{"headline":"Hi","publishedOn":"someday"}""", "Published on must be a date.")]
    [TestCase("""{"headline":"Hi","style":"neon"}""", "Style must be one of: light, dark.")]
    [TestCase("""{"headline":"Hi","style":"retired"}""", "Style must be one of: light, dark.")]
    [TestCase("""{"headline":42}""", "Headline must be text.")]
    [TestCase("""{"headline":"Hi","items":{"question":"Why?"}}""", "Items must be a list of rows.")]
    [TestCase("""{"headline":"Hi","items":["Why?"]}""", "Items row 1 must be an object.")]
    public void A_value_of_the_wrong_type_fails(string json, string error)
    {
        New(json).Should().Equal(error);
    }

    [Test]
    public void Repeater_rows_are_checked_by_sub_field()
    {
        New("""{"headline":"Hi","items":[{"question":"Ok"},{"votes":"many"}]}""")
            .Should()
            .Equal("Items row 2: Question is required. Items row 2: Votes must be a number.");
    }

    [Test]
    public void Keys_that_are_not_fields_are_not_checked()
    {
        New("""{"headline":"Hi","legacyKey":{"any":"thing"},"minHeight":1}""").Should().BeEmpty();
    }

    [Test]
    public void Settings_must_be_a_json_object()
    {
        New("not json").Should().Equal("Invalid JSON format.");
        New("[]").Should().Equal("Settings must be a JSON object.");
    }

    [Test]
    public void Empty_settings_are_treated_as_an_empty_object()
    {
        WidgetSettings.Validate("", [Fields[1]], []).Should().BeEmpty();
    }

    [Test]
    public void Unchanged_settings_of_a_saved_widget_are_accepted_unchecked()
    {
        const string legacy = """{"minHeight":"640px","color":"red"}""";

        WidgetSettings.Validate(legacy, Fields, [legacy]).Should().BeEmpty();
    }

    [Test]
    public void Unchanged_values_are_accepted_but_changed_values_are_checked()
    {
        const string saved = """{"headline":null,"minHeight":"640px","color":"red"}""";

        WidgetSettings
            .Validate("""{"color":"red","minHeight":"640px","extra":1}""", Fields, [saved])
            .Should()
            .BeEmpty();
        WidgetSettings
            .Validate("""{"headline":null,"minHeight":"720px","color":"red"}""", Fields, [saved])
            .Should()
            .Equal("Min height must be a number.");
    }

    [Test]
    public void A_value_equal_to_either_the_draft_or_the_published_copy_is_accepted()
    {
        WidgetSettings
            .Validate("""{"headline":"Hi","color":"red"}""", Fields, ["""{"headline":"Hi","color":"#000000"}""", """{"color":"red"}"""])
            .Should()
            .BeEmpty();
    }

    [Test]
    public void Settings_of_a_new_widget_are_fully_checked()
    {
        WidgetSettings.Validate("""{"minHeight":"640px"}""", Fields, []).Should().HaveCount(2);
    }

    [Test]
    public void Built_in_defaults_pass_their_own_fields()
    {
        foreach (var type in BuiltInWidgetType.WidgetTypes)
        {
            var defaults = type.Fields.Where(f => f.DefaultValue is not null)
                .ToDictionary(f => f.DeveloperName, f => f.DefaultValue!.Value);
            var errors = WidgetSettings.Validate(JsonSerializer.Serialize(defaults), type.Fields, []);

            errors.Should().OnlyContain(e => e.EndsWith(" is required."), type.DeveloperName);
        }
    }
}
