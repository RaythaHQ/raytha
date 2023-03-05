using FluentAssertions;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class StringFieldValueTests
{
    private string? stringValue = "The sly dog jumped over the crazy fox.";

    [Test]
    public void ShouldReturnCorrectFieldValue()
    {
        var type = BaseFieldType.From("single_line_text");
        var fieldValue = type.FieldValueFrom(stringValue);
        fieldValue.Should().BeOfType<StringFieldValue>();
        ((string?)fieldValue.Value).Should().Be(stringValue);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Should().BeOfType<StringFieldValue>();
        ((string?)fieldValue.Value).Should().BeNull();
    }

    [Test]
    public void ShouldTextMatch()
    {
        var type = BaseFieldType.From("single_line_text");
        var fieldValue = type.FieldValueFrom(stringValue);
        fieldValue.Text.Should().Be(stringValue);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Text.Should().Be(string.Empty);
    }

    [Test]
    public void ShouldHaveCorrectHasValue()
    {
        var type = BaseFieldType.From("single_line_text");
        var fieldValue = type.FieldValueFrom(stringValue);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.HasValue.Should().BeFalse();
    }
}
