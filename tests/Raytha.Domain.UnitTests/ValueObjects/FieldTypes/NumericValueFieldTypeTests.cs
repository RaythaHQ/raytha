using FluentAssertions;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects.FieldTypes;

public class NumericValueFieldTypeTests
{
    [Test]
    public void FieldValueFrom_ShouldReturnDecimalFieldValue_WhenGivenValidNumber()
    {
        // Arrange
        var type = new NumberFieldType();
        var value = 123.45m;

        // Act
        var result = type.FieldValueFrom(value);

        // Assert
        result.Should().BeOfType<DecimalFieldValue>();
        ((decimal?)result.Value).Should().Be(123.45m);
    }

    [Test]
    public void FieldValueFrom_ShouldReturnDecimalFieldValue_WhenGivenStringNumber()
    {
        // Arrange
        var type = new NumberFieldType();
        var value = "123.45";

        // Act
        var result = type.FieldValueFrom(value);

        // Assert
        result.Should().BeOfType<DecimalFieldValue>();
        ((decimal?)result.Value).Should().Be(123.45m);
    }

    [Test]
    public void FieldValueFrom_ShouldReturnEmpty_WhenGivenNull()
    {
        // Arrange
        var type = new NumberFieldType();
        object value = null;

        // Act
        var result = type.FieldValueFrom(value);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Test]
    public void FieldValueFrom_ShouldReturnEmpty_WhenGivenEmptyString()
    {
        // Arrange
        var type = new NumberFieldType();
        var value = string.Empty;

        // Act
        var result = type.FieldValueFrom(value);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Test]
    public void FieldValueFrom_ShouldThrowException_WhenGivenInvalidString()
    {
        // Arrange
        var type = new NumberFieldType();
        var value = "not a number";

        // Act
        Action act = () => type.FieldValueFrom(value);

        // Assert
        act.Should().Throw<FormatException>();
    }

}

