using System.Text.Json;
using FluentAssertions;
using Raytha.Application.Themes.WidgetTemplates;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Themes.WidgetTemplates;

public class WidgetFieldDefinitionsTests
{
    private static FieldDefinition Text(string name) =>
        new() { DeveloperName = name, Label = name, FieldType = "single_line_text" };

    private static List<string> Errors(params FieldDefinition[] fields) =>
        WidgetFieldDefinitions.Validate(fields).ToList();

    [Test]
    public void Every_allowed_field_type_is_accepted()
    {
        Errors(
                Text("title"),
                new() { DeveloperName = "body", Label = "Body", FieldType = "long_text" },
                new() { DeveloperName = "rich", Label = "Rich", FieldType = "wysiwyg" },
                new() { DeveloperName = "count", Label = "Count", FieldType = "number" },
                new() { DeveloperName = "on", Label = "On", FieldType = "checkbox" },
                new() { DeveloperName = "day", Label = "Day", FieldType = "date" },
                new()
                {
                    DeveloperName = "size",
                    Label = "Size",
                    FieldType = "dropdown",
                    Choices = [new ContentTypeFieldChoice { DeveloperName = "s", Label = "Small" }],
                },
                new()
                {
                    DeveloperName = "tone",
                    Label = "Tone",
                    FieldType = "radio",
                    Choices = [new ContentTypeFieldChoice { DeveloperName = "calm", Label = "Calm" }],
                },
                new() { DeveloperName = "tint", Label = "Tint", FieldType = "color" },
                new() { DeveloperName = "rows", Label = "Rows", FieldType = "repeater", SubFields = [Text("cell")] },
                new() { DeveloperName = "photo", Label = "Photo", FieldType = "image" },
                new() { DeveloperName = "contentType", Label = "Type", FieldType = "content_type" },
                new() { DeveloperName = "viewId", Label = "View", FieldType = "view", ContentTypeField = "contentType" }
            )
            .Should()
            .BeEmpty();
    }

    [Test]
    public void Developer_names_must_match_the_settings_key_pattern()
    {
        Errors(Text("1st"), Text("has-dash"), Text("") with { Label = "Blank" }).Should().HaveCount(3).And.OnlyContain(e => e.Contains("must start with a letter"));
        Errors(Text("camelCase_1")).Should().BeEmpty();
    }

    [Test]
    public void Developer_names_must_be_unique_ignoring_case()
    {
        Errors(Text("title"), Text("Title")).Should().Equal("Developer name 'title' is used by more than one field.");
    }

    [Test]
    public void A_label_is_required()
    {
        Errors(Text("title") with { Label = " " }).Should().Equal("Field 'title' needs a label.");
    }

    [Test]
    public void An_unsupported_field_type_is_rejected()
    {
        Errors(Text("owner") with { FieldType = "one_to_one_relationship" })
            .Should()
            .Equal("Field 'owner' has an unsupported field type 'one_to_one_relationship'.");
    }

    [Test]
    public void Choice_fields_need_valid_unique_choices_and_other_fields_have_none()
    {
        var choice = new ContentTypeFieldChoice { DeveloperName = "a", Label = "A" };

        Errors(Text("size") with { FieldType = "dropdown" }).Should().Equal("Field 'size' needs at least one choice.");
        Errors(Text("size") with { FieldType = "radio", Choices = [choice, choice] })
            .Should()
            .Equal("Field 'size' has duplicate choice developer names.");
        Errors(Text("size") with { FieldType = "dropdown", Choices = [new ContentTypeFieldChoice { Label = "A" }] })
            .Should()
            .Equal("Field 'size' has a choice without a label or developer name.");
        Errors(Text("size") with { Choices = [choice] })
            .Should()
            .Equal("Field 'size' cannot have choices; only dropdown and radio fields do.");
    }

    [Test]
    public void Repeaters_need_valid_scalar_sub_fields()
    {
        Errors(Text("items") with { FieldType = "repeater" }).Should().Equal("Field 'items' needs at least one sub-field.");
        Errors(
                Text("items") with
                {
                    FieldType = "repeater",
                    SubFields = [Text("q"), Text("Q"), Text("inner") with { FieldType = "repeater", SubFields = [Text("x")] }],
                }
            )
            .Should()
            .Equal(
                "Field 'items': Developer name 'q' is used by more than one field.",
                "Field 'items': Field 'inner' cannot be a repeater inside a repeater."
            );
        Errors(Text("title") with { SubFields = [Text("x")] })
            .Should()
            .Equal("Field 'title' cannot have sub-fields; only repeater fields do.");
    }

    [Test]
    public void A_view_field_must_name_a_sibling_content_type_field()
    {
        var view = new FieldDefinition { DeveloperName = "viewId", Label = "View", FieldType = "view", ContentTypeField = "source" };

        Errors(view).Should().Equal("Field 'View' must name a content type field of the same template in ContentTypeField.");
        Errors(view, Text("source")).Should().ContainSingle();
        Errors(view, Text("source") with { FieldType = "content_type" }).Should().BeEmpty();
        Errors(Text("title") with { ContentTypeField = "source" })
            .Should()
            .Equal("Field 'title' cannot set ContentTypeField; only view fields do.");
    }

    [Test]
    public void A_default_value_must_suit_its_field_type()
    {
        Errors(Text("count") with { FieldType = "number", DefaultValue = JsonSerializer.SerializeToElement("10") })
            .Should()
            .Equal("Default value: count must be a number.");
        Errors(Text("count") with { FieldType = "number", IsRequired = true, DefaultValue = JsonSerializer.SerializeToElement(10) })
            .Should()
            .BeEmpty();
    }
}
