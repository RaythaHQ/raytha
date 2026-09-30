using System.Text.Json;
using FluentAssertions;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class ColorFieldTypeTests
{
    [Test]
    public void Color_is_a_supported_type_with_its_label()
    {
        var type = BaseFieldType.From("color");

        type.Should().BeOfType<ColorFieldType>();
        type.Label.Should().Be("Color");
        type.HasChoices.Should().BeFalse();
        type.IsSortable.Should().BeTrue();
        type.IsSearchable.Should().BeTrue();
        type.StoresJsonArray.Should().BeFalse();
    }

    [Test]
    public void Color_supports_equality_and_emptiness_operators()
    {
        BaseFieldType
            .Color.SupportedConditionOperators.Select(o => o.DeveloperName)
            .Should()
            .Equal(
                ConditionOperator.EQUALS,
                ConditionOperator.NOT_EQUALS,
                ConditionOperator.IS_EMPTY,
                ConditionOperator.IS_NOT_EMPTY
            );
    }

    [Test]
    [TestCase("#ff8800", "#ff8800")]
    [TestCase("#FF8800", "#ff8800")]
    [TestCase("  #Ff8800 ", "#ff8800")]
    [TestCase("#f80", "#ff8800")]
    [TestCase("#F80", "#ff8800")]
    public void Color_values_normalize_to_lowercase_rrggbb(string input, string expected)
    {
        var value = BaseFieldType.Color.FieldValueFrom(input);

        value.Should().BeOfType<ColorFieldValue>();
        ((string)value.Value).Should().Be(expected);
        value.Text.Should().Be(expected);
        value.HasValue.Should().BeTrue();
    }

    [Test]
    [TestCase("ff8800")]
    [TestCase("#ff880")]
    [TestCase("#ff88000")]
    [TestCase("#gg8800")]
    [TestCase("red")]
    [TestCase("rgb(255, 136, 0)")]
    public void Color_rejects_anything_that_is_not_hex(string input)
    {
        FluentActions
            .Invoking(() => BaseFieldType.Color.FieldValueFrom(input))
            .Should()
            .Throw<FormatException>();
    }

    [Test]
    public void Empty_color_has_no_value()
    {
        foreach (var input in new object?[] { null, "", "   ", JsonDocument.Parse("null").RootElement })
        {
            var value = new ColorFieldValue(input);
            value.HasValue.Should().BeFalse();
            value.Text.Should().BeEmpty();
        }
    }

    [Test]
    public void Color_reads_a_json_string_element()
    {
        var element = JsonDocument.Parse("\"#ABCDEF\"").RootElement;

        new ColorFieldValue(element).Text.Should().Be("#abcdef");
    }
}
