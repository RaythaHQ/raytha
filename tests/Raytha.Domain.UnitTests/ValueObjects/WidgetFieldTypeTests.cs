using System.Text.Json;
using FluentAssertions;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class WidgetFieldTypeTests
{
    [Test]
    public void The_allowed_types_are_exactly_the_widget_field_types()
    {
        WidgetFieldType
            .SupportedTypes.Select(t => t.DeveloperName)
            .Should()
            .Equal(
                "single_line_text",
                "long_text",
                "wysiwyg",
                "number",
                "checkbox",
                "date",
                "dropdown",
                "radio",
                "color",
                "repeater",
                "image",
                "content_type",
                "view"
            );
    }

    [Test]
    public void Types_are_equal_by_developer_name()
    {
        WidgetFieldType.From("color").Should().Be(WidgetFieldType.Color);
        ((string)WidgetFieldType.Repeater).Should().Be("repeater");
        WidgetFieldType.IsSupported("one_to_one_relationship").Should().BeFalse();
        FluentActions.Invoking(() => WidgetFieldType.From("attachment")).Should().Throw<UnsupportedFieldTypeException>();
    }

    [Test]
    public void Only_dropdown_and_radio_have_choices_and_only_repeater_cannot_nest()
    {
        WidgetFieldType.SupportedTypes.Where(t => t.HasChoices).Select(t => t.DeveloperName).Should().Equal("dropdown", "radio");
        WidgetFieldType.SupportedTypes.Where(t => !t.AllowedInRepeater).Select(t => t.DeveloperName).Should().Equal("repeater");
    }

    [Test]
    public void Values_keep_their_json_type()
    {
        var number = new FieldDefinition { DeveloperName = "n", Label = "N", FieldType = "number" };
        var checkbox = new FieldDefinition { DeveloperName = "c", Label = "C", FieldType = "checkbox" };

        WidgetFieldType.Validate(number, JsonSerializer.SerializeToElement(4.5)).Should().BeEmpty();
        WidgetFieldType.Validate(number, JsonSerializer.SerializeToElement("4.5")).Should().Equal("N must be a number.");
        WidgetFieldType.Validate(checkbox, JsonSerializer.SerializeToElement(true)).Should().BeEmpty();
        WidgetFieldType.Validate(checkbox, JsonSerializer.SerializeToElement("true")).Should().Equal("C must be true or false.");
    }

    [Test]
    public void Built_in_templates_are_created_and_reset_with_their_fields()
    {
        var themeId = Guid.NewGuid();
        var template = BuiltInWidgetType.FAQ.CreateTemplate(themeId);

        template.ThemeId.Should().Be(themeId);
        template.IsBuiltInTemplate.Should().BeTrue();
        template._FieldsJson.Should().Be(FieldDefinition.ListToJson(BuiltInWidgetType.FAQ.Fields));

        template.Content = "custom";
        template.Fields = [];
        BuiltInWidgetType.FAQ.ResetTemplate(template);

        template.Content.Should().Be(BuiltInWidgetType.FAQ.DefaultTemplateContent);
        template._FieldsJson.Should().Be(FieldDefinition.ListToJson(BuiltInWidgetType.FAQ.Fields));
        BuiltInWidgetType.Find("faq").Should().Be(BuiltInWidgetType.FAQ);
        BuiltInWidgetType.Find("callout").Should().BeNull();
    }
}
