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

    [TestCase("2/30/2026")]
    [TestCase("sometime in spring")]
    public void A_stored_date_that_does_not_parse_reads_as_empty_and_keeps_its_text(string stored)
    {
        var fieldValue = BaseFieldType.Date.StoredValueFrom(stored);

        fieldValue.Should().BeOfType<DateTimeFieldValue>();
        fieldValue.HasValue.Should().BeFalse();
        ((DateTime?)fieldValue.Value).Should().BeNull();
        fieldValue.Text.Should().Be(stored);
    }

    [Test]
    public void A_stored_date_that_parses_reads_like_a_written_one()
    {
        var fieldValue = BaseFieldType.Date.StoredValueFrom("2026-01-13");

        ((DateTime?)fieldValue.Value).Should().Be(new DateTime(2026, 1, 13));
    }

    [Test]
    public void A_date_that_does_not_parse_is_still_rejected_when_written()
    {
        var act = () => BaseFieldType.Date.FieldValueFrom("2/30/2026");

        act.Should().Throw<FormatException>();
    }
}
