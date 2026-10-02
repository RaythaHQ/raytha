using FluentAssertions;
using Raytha.Application.ContentTypes;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentTypes;

public class RepeaterSubFieldsTests
{
    [Test]
    public void A_repeater_needs_at_least_one_sub_field()
    {
        RepeaterSubFields.Errors([]).Should().Equal("A repeater needs at least one sub-field.");
        RepeaterSubFields.Errors(null).Should().Equal("A repeater needs at least one sub-field.");
    }

    [Test]
    public void Valid_scalar_sub_fields_have_no_errors()
    {
        RepeaterSubFields
            .Errors(
                [
                    Sub("Question", "question", "single_line_text"),
                    Sub("Answer", "answer", "wysiwyg"),
                    Sub("Accent", "accent", "color"),
                    Sub("Order", "order", "number"),
                    Sub("Size", "size", "dropdown", ("Small", "small"), ("Large", "large")),
                ]
            )
            .Should()
            .BeEmpty();
    }

    [Test]
    [TestCase("repeater")]
    [TestCase("multiple_select")]
    [TestCase("one_to_one_relationship")]
    [TestCase("not_a_type")]
    public void Nested_relationship_and_multi_value_types_are_rejected(string fieldType)
    {
        RepeaterSubFields
            .Errors([Sub("Thing", "thing", fieldType)])
            .Should()
            .Equal($"'Thing' cannot use the '{fieldType}' field type in a repeater.");
    }

    [Test]
    public void Sub_fields_need_a_label_and_a_letter_first_developer_name()
    {
        var errors = RepeaterSubFields.Errors(
            [Sub("", "question", "single_line_text"), Sub("Year", "1st_year", "number")]
        );

        errors.Should().Contain("Sub-field 1 needs a label.");
        errors
            .Should()
            .Contain(
                "'Year' needs a developer name that starts with a letter and uses only letters, numbers, and underscores."
            );
    }

    [Test]
    public void Developer_names_must_be_unique_after_normalizing()
    {
        RepeaterSubFields
            .Errors([Sub("Question", "question", "single_line_text"), Sub("Other", "Question", "long_text")])
            .Should()
            .Equal("Sub-field developer names must be unique. Duplicates: question");
    }

    [Test]
    public void Choice_sub_fields_need_labeled_unique_choices()
    {
        RepeaterSubFields
            .Errors([Sub("Size", "size", "radio")])
            .Should()
            .Equal("'Size' needs at least one choice.");

        RepeaterSubFields
            .Errors([Sub("Size", "size", "radio", ("Small", "small"), ("", "small"))])
            .Should()
            .Equal("'Size' has a choice without a label.", "'Size' has choices with the same developer name.");
    }

    [Test]
    public void Normalize_snake_cases_names_and_drops_choices_on_non_choice_types()
    {
        var normalized = RepeaterSubFields.Normalize(
            [
                Sub(" Question ", "Question Text", "SINGLE_LINE_TEXT", ("Stray", "stray")),
                Sub("Size", "size", "dropdown", ("Small", "Small One")),
            ]
        );

        normalized[0].DeveloperName.Should().Be("question_text");
        normalized[0].Label.Should().Be("Question");
        normalized[0].FieldType.Should().Be("single_line_text");
        normalized[0].Choices.Should().BeEmpty();
        normalized[1].Choices.Single().DeveloperName.Should().Be("small_one");
    }

    private static FieldDefinition Sub(
        string label,
        string developerName,
        string fieldType,
        params (string Label, string DeveloperName)[] choices
    ) =>
        new()
        {
            Label = label,
            DeveloperName = developerName,
            FieldType = fieldType,
            Choices = choices
                .Select(c => new ContentTypeFieldChoice { Label = c.Label, DeveloperName = c.DeveloperName })
                .ToList(),
        };
}
