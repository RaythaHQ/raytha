using FluentAssertions;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class DecimalFieldValueTests
{
    private decimal? decimalValue = 1.0m;
    private string decimalValueAsString = "1.0";

    [Test]
    public void ShouldReturnCorrectFieldValue()
    {
        var type = BaseFieldType.From("number");
        var fieldValue = type.FieldValueFrom(decimalValue);
        fieldValue.Should().BeOfType<DecimalFieldValue>();
        ((decimal?)fieldValue.Value).Should().Be(decimalValue);

        fieldValue = type.FieldValueFrom(decimalValueAsString);
        fieldValue.Should().BeOfType<DecimalFieldValue>();
        ((decimal?)fieldValue.Value).Should().Be(decimalValue);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Should().BeOfType<DecimalFieldValue>();
        ((decimal?)fieldValue.Value).Should().BeNull();
    }

    [Test]
    public void ShouldTextMatch()
    {
        var type = BaseFieldType.From("number");
        var fieldValue = type.FieldValueFrom(decimalValue);
        fieldValue.Text.Should().Be(decimalValueAsString);

        fieldValue = type.FieldValueFrom(decimalValueAsString);
        fieldValue.Text.Should().Be(decimalValueAsString);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Text.Should().Be(string.Empty);
    }

    [Test]
    public void ShouldHaveCorrectHasValue()
    {
        var type = BaseFieldType.From("number");
        var fieldValue = type.FieldValueFrom(decimalValue);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(decimalValueAsString);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.HasValue.Should().BeFalse();
    }
}
