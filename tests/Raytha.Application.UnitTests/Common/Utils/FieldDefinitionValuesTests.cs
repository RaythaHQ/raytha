using System.Text.Json;
using FluentAssertions;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.UnitTests.Common.Utils;

public class FieldDefinitionValuesTests
{
    private static readonly ContentTypeField Faq = new()
    {
        DeveloperName = "faq",
        Label = "FAQ",
        FieldType = BaseFieldType.Repeater,
        SubFields =
        [
            new FieldDefinition
            {
                DeveloperName = "question",
                Label = "Question",
                FieldType = "single_line_text",
                IsRequired = true,
            },
            new FieldDefinition { DeveloperName = "order", Label = "Order", FieldType = "number" },
            new FieldDefinition { DeveloperName = "featured", Label = "Featured", FieldType = "checkbox" },
            new FieldDefinition { DeveloperName = "accent", Label = "Accent", FieldType = "color" },
            new FieldDefinition
            {
                DeveloperName = "size",
                Label = "Size",
                FieldType = "radio",
                Choices = [new ContentTypeFieldChoice { Label = "Small", DeveloperName = "small" }],
            },
        ],
    };

    private static readonly ContentTypeField Accent = new()
    {
        DeveloperName = "accent",
        Label = "Accent",
        FieldType = BaseFieldType.Color,
    };

    [Test]
    public void Stored_content_canonicalizes_color_and_repeater_and_leaves_other_fields_alone()
    {
        var content = new Dictionary<string, dynamic>
        {
            ["title"] = Json("\"  Hello \""),
            ["accent"] = Json("\"#F80\""),
            ["faq"] = Json(
                """[{"question":"Why?","order":"2","featured":"true","accent":"#ABCDEF","size":"small","stray":"x"}]"""
            ),
        };

        var stored = FieldDefinitionValues.ToStoredContent([Faq, Accent, Title()], content);

        ((JsonElement)stored["title"]).GetString().Should().Be("  Hello ");
        ((string)stored["accent"]).Should().Be("#ff8800");
        var rows = (List<Dictionary<string, object?>>)stored["faq"];
        rows.Single()
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object?>
                {
                    ["question"] = "Why?",
                    ["order"] = 2m,
                    ["featured"] = true,
                    ["accent"] = "#abcdef",
                    ["size"] = "small",
                }
            );
    }

    [Test]
    public void Stored_rows_serialize_as_a_plain_json_array()
    {
        var stored = FieldDefinitionValues.ToStoredContent(
            [Faq],
            new Dictionary<string, dynamic> { ["faq"] = Json("""[{"question":"Why?"}]""") }
        );

        JsonSerializer
            .Serialize(stored)
            .Should()
            .Be("""{"faq":[{"question":"Why?","order":null,"featured":false,"accent":null,"size":null}]}""");
    }

    [Test]
    public void Row_errors_name_the_row_and_the_sub_field()
    {
        var value = Faq.FieldType.FieldValueFrom(
            Json("""[{"question":"Why?"},{"question":"","order":"many","accent":"blue","size":"huge"}]""")
        );

        FieldDefinitionValues
            .RowErrors(Faq, value, enforceRequired: true)
            .Should()
            .Equal(
                "FAQ, row 2: 'Question' field is required.",
                "FAQ, row 2: 'Order' is an invalid format.",
                "FAQ, row 2: 'Accent' is an invalid format.",
                "FAQ, row 2: 'Size' must be one of its choices."
            );
    }

    [Test]
    public void Drafts_skip_required_sub_fields_but_still_check_formats()
    {
        var value = Faq.FieldType.FieldValueFrom(Json("""[{"question":"","accent":"blue"}]"""));

        FieldDefinitionValues
            .RowErrors(Faq, value, enforceRequired: false)
            .Should()
            .Equal("FAQ, row 1: 'Accent' is an invalid format.");
    }

    [Test]
    public void Non_repeater_values_have_no_row_errors()
    {
        FieldDefinitionValues
            .RowErrors(Accent, Accent.FieldType.FieldValueFrom("#fff"), enforceRequired: true)
            .Should()
            .BeEmpty();
    }

    private static ContentTypeField Title() =>
        new()
        {
            DeveloperName = "title",
            Label = "Title",
            FieldType = BaseFieldType.SingleLineText,
        };

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
