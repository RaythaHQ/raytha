using FluentAssertions;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects.FieldTypes;

public class DateFieldTypeTests
{
    [Test]
    public void FieldValueFrom_ShouldReturnDateTimeFieldValue_WhenGivenValidDate()
    {
        // Arrange
        var type = new DateFieldType();
        var value = DateTime.Now;

        // Act
        var result = type.FieldValueFrom(value);

        // Assert
        result.Should().BeOfType<DateTimeFieldValue>();
        ((DateTime?)result.Value).Should().BeCloseTo(value, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void FieldValueFrom_ShouldReturnDateTimeFieldValue_WhenGivenStringDate()
    {
        // Arrange
        var type = new DateFieldType();
        var value = "2023-01-01";

        // Act
        var result = type.FieldValueFrom(value);

        // Assert
        result.Should().BeOfType<DateTimeFieldValue>();
        ((DateTime?)result.Value).Should().Be(new DateTime(2023, 1, 1));
    }

    [Test]
    public void FieldValueFrom_ShouldReturnEmpty_WhenGivenNull()
    {
        // Arrange
        var type = new DateFieldType();
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
        var type = new DateFieldType();
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
        var type = new DateFieldType();
        var value = "not a date";

        // Act
        Action act = () => type.FieldValueFrom(value);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Test]
    [TestCase("MM/dd/yyyy", 101, "ASC")]
    [TestCase("dd/MM/yyyy", 103, "DESC")]
    [TestCase("unknown", 0, "ASC")]
    public void SqlServerOrderByExpression_ShouldReturnCorrectSql(string dateFormat, int expectedStyle, string order)
    {
        // Arrange
        var type = new DateFieldType();
        var table = "t";
        var column = "c";
        var key = "k";

        // Act
        var result = type.SqlServerOrderByExpression(table, column, key, dateFormat, order);

        // Assert
        result.Should().Be($" TRY_CONVERT(datetime, JSON_VALUE({table}.{column}, '$.{key}'), {expectedStyle}) {order} ");
    }

    [Test]
    [TestCase("MM/DD/YYYY", "ASC")]
    [TestCase("DD/MM/YYYY", "DESC")]
    public void PostgresOrderByExpression_ShouldReturnCorrectSql(string dateFormat, string order)
    {
        // Arrange
        var type = new DateFieldType();
        var table = "t";
        var column = "c";
        var key = "k";

        // Act
        var result = type.PostgresOrderByExpression(table, column, key, dateFormat, order);

        // Assert
        var expected = $@"
        TO_DATE(
            NULLIF({table}.""{column}""->>'{key}', ''),
            '{dateFormat}'
        ) {order}";
        
        result.Should().Be(expected);
    }
}

