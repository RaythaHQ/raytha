using FluentAssertions;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class DateTimeFieldValueTests
{
    private DateTime? dateValue = new DateTime(2000, 1, 1);
    private string dateValueAsString = new DateTime(2000, 1, 1).ToString();

    [Test]
    public void ShouldReturnCorrectFieldValue()
    {
        var type = BaseFieldType.From("date");
        var fieldValue = type.FieldValueFrom(dateValue);
        fieldValue.Should().BeOfType<DateTimeFieldValue>();
        ((DateTime?)fieldValue.Value).Should().Be(dateValue);

        fieldValue = type.FieldValueFrom(dateValueAsString);
        fieldValue.Should().BeOfType<DateTimeFieldValue>();
        ((DateTime?)fieldValue.Value).Should().Be(dateValue);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Should().BeOfType<DateTimeFieldValue>();
        ((DateTime?)fieldValue.Value).Should().BeNull();
    }

    [Test]
    public void ShouldTextMatch()
    {
        var type = BaseFieldType.From("date");
        var fieldValue = type.FieldValueFrom(dateValue);
        fieldValue.Text.Should().Be(dateValueAsString);

        fieldValue = type.FieldValueFrom(dateValueAsString);
        fieldValue.Text.Should().Be(dateValueAsString);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Text.Should().Be(string.Empty);
    }

    [Test]
    public void ShouldHaveCorrectHasValue()
    {
        var type = BaseFieldType.From("date");
        var fieldValue = type.FieldValueFrom(dateValue);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(dateValueAsString);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.HasValue.Should().BeFalse();
    }
}
