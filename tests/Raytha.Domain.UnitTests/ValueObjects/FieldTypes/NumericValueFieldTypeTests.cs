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

    [Test]
    public void SqlServerOrderByExpression_ShouldReturnCorrectSql()
    {
        // Arrange
        var type = new NumberFieldType();
        var table = "t";
        var column = "c";
        var key = "k";
        var order = "ASC";

        // Act
        var result = type.SqlServerOrderByExpression(table, column, key, order);

        // Assert
        result.Should().Be($" CASE WHEN ISNUMERIC(JSON_VALUE({table}.{column}, '$.{key}')) = 1 THEN CAST(JSON_VALUE({table}.{column}, '$.{key}') AS decimal) ELSE NULL END {order}, JSON_VALUE({table}.{column}, '$.{key}') {order} ");
    }

    [Test]
    public void PostgresOrderByExpression_ShouldReturnCorrectSql()
    {
        // Arrange
        var type = new NumberFieldType();
        var table = "t";
        var column = "c";
        var key = "k";
        var order = "ASC";

        // Act
        var result = type.PostgresOrderByExpression(table, column, key, order);

        // Assert
        result.Should().Be($" CASE WHEN ({table}.\"{column}\"->>'{key}') ~ '^[0-9]+(\\.[0-9]+)?$' THEN ({table}.\"{column}\"->> '{key}')::decimal ELSE NULL END {order}, {table}.\"{column}\"->>'{key}' {order} ");
    }
}

