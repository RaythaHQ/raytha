using System.Text.Json;
using FluentAssertions;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class RepeaterFieldTypeTests
{
    private const string TwoRows =
        """[{"question":"Why?","answer":"Because.","order":2,"featured":true},{"question":"How?","answer":null}]""";

    [Test]
    public void Repeater_is_a_supported_type_that_is_neither_sortable_nor_searchable()
    {
        var type = BaseFieldType.From("repeater");

        type.Should().BeOfType<RepeaterFieldType>();
        type.Label.Should().Be("Repeater");
        type.HasChoices.Should().BeFalse();
        type.IsSortable.Should().BeFalse();
        type.IsSearchable.Should().BeFalse();
        type.StoresJsonArray.Should().BeTrue();
    }

    [Test]
    public void Repeater_supports_only_emptiness_operators()
    {
        BaseFieldType
            .Repeater.SupportedConditionOperators.Select(o => o.DeveloperName)
            .Should()
            .Equal(ConditionOperator.IS_EMPTY, ConditionOperator.IS_NOT_EMPTY);
    }

    [Test]
    public void Repeater_sub_field_types_are_scalar_only()
    {
        RepeaterFieldType
            .SubFieldTypes.Should()
            .BeEquivalentTo(
                "single_line_text",
                "long_text",
                "wysiwyg",
                "number",
                "checkbox",
                "date",
                "dropdown",
                "radio",
                "color",
                "attachment"
            );
    }

    [Test]
    public void Repeater_parses_rows_from_json_text_and_elements()
    {
        foreach (object input in new object[] { TwoRows, JsonDocument.Parse(TwoRows).RootElement })
        {
            var value = (RepeaterFieldValue)BaseFieldType.Repeater.FieldValueFrom(input);

            value.HasValue.Should().BeTrue();
            var rows = value.Rows();
            rows.Should().HaveCount(2);
            rows[0]["question"].Should().Be("Why?");
            rows[0]["order"].Should().Be(2m);
            rows[0]["featured"].Should().Be(true);
            rows[1]["answer"].Should().BeNull();
        }
    }

    [Test]
    public void Repeater_value_is_a_list_of_dictionaries_for_liquid()
    {
        var value = BaseFieldType.Repeater.FieldValueFrom(TwoRows);

        object rows = value.Value;
        rows.Should().BeAssignableTo<IEnumerable<Dictionary<string, object?>>>();
    }

    [Test]
    public void Repeater_text_round_trips_as_json()
    {
        var value = BaseFieldType.Repeater.FieldValueFrom(TwoRows);

        var reparsed = (RepeaterFieldValue)BaseFieldType.Repeater.FieldValueFrom(value.Text);

        reparsed.Text.Should().Be(value.Text);
        reparsed.Rows().Should().HaveCount(2);
    }

    [Test]
    public void Repeater_parses_rows_already_in_clr_form()
    {
        var rows = new List<Dictionary<string, object?>> { new() { ["question"] = "Why?" } };

        var value = (RepeaterFieldValue)BaseFieldType.Repeater.FieldValueFrom(rows);

        value.Rows().Single()["question"].Should().Be("Why?");
    }

    [Test]
    public void Empty_repeater_has_no_value_and_blank_text()
    {
        foreach (var input in new object?[] { null, "", "[]", JsonDocument.Parse("[]").RootElement, JsonDocument.Parse("null").RootElement })
        {
            var value = new RepeaterFieldValue(input);
            value.HasValue.Should().BeFalse();
            value.Text.Should().BeEmpty();
        }
    }

    [Test]
    [TestCase("not json")]
    [TestCase("{\"question\":\"Why?\"}")]
    [TestCase("[\"Why?\"]")]
    [TestCase("[{\"question\":[\"nested\"]}]")]
    [TestCase("[{\"question\":{\"nested\":true}}]")]
    public void Repeater_rejects_anything_but_an_array_of_flat_objects(string input)
    {
        FluentActions
            .Invoking(() => BaseFieldType.Repeater.FieldValueFrom(input))
            .Should()
            .Throw<FormatException>();
    }

    [Test]
    public void Repeater_emptiness_sql_treats_missing_null_and_empty_array_as_empty()
    {
        var sql = BaseFieldType.Repeater.LikeJsonValue("t", "_PublishedContent", "faq", "[]");

        sql.Should().Be("(COALESCE(t.\"_PublishedContent\"->'faq', '[]'::jsonb) IN ('[]'::jsonb, 'null'::jsonb, '\"\"'::jsonb))");
    }

    [Test]
    public void Repeater_refuses_to_emit_a_text_filter()
    {
        FluentActions
            .Invoking(() => BaseFieldType.Repeater.LikeJsonValue("t", "_PublishedContent", "faq", "%why%"))
            .Should()
            .Throw<NotSupportedException>();
    }
}
