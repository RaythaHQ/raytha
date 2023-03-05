using FluentAssertions;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class BooleanFieldValueTests
{
    private bool trueValue = true;
    private bool falseValue = false;

    private string trueValueAsString = "True";
    private string falseValueAsString = "False";


    [Test]
    public void ShouldReturnCorrectFieldValue()
    {
        var type = BaseFieldType.From("checkbox");
        var fieldValue = type.FieldValueFrom(trueValue);
        fieldValue.Should().BeOfType<BooleanFieldValue>();
        ((bool?)fieldValue.Value).Should().BeTrue();

        fieldValue = type.FieldValueFrom(trueValueAsString);
        fieldValue.Should().BeOfType<BooleanFieldValue>();
        ((bool?)fieldValue.Value).Should().BeTrue();

        fieldValue = type.FieldValueFrom(falseValue);
        fieldValue.Should().BeOfType<BooleanFieldValue>();
        ((bool?)fieldValue.Value).Should().BeFalse();

        fieldValue = type.FieldValueFrom(falseValueAsString);
        fieldValue.Should().BeOfType<BooleanFieldValue>();
        ((bool?)fieldValue.Value).Should().BeFalse();

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Should().BeOfType<BooleanFieldValue>();
        ((bool?)fieldValue.Value).Should().BeNull();
    }

    [Test]
    public void ShouldTextMatch()
    {
        var type = BaseFieldType.From("checkbox");
        var fieldValue = type.FieldValueFrom(trueValue);
        fieldValue.Text.Should().Be(trueValueAsString);

        fieldValue = type.FieldValueFrom(trueValueAsString);
        fieldValue.Text.Should().Be(trueValueAsString);

        fieldValue = type.FieldValueFrom(falseValue);
        fieldValue.Text.Should().Be(falseValueAsString);

        fieldValue = type.FieldValueFrom(falseValueAsString);
        fieldValue.Text.Should().Be(falseValueAsString);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Text.Should().Be(string.Empty);
    }

    [Test]
    public void ShouldHaveCorrectHasValue()
    {
        var type = BaseFieldType.From("checkbox");
        var fieldValue = type.FieldValueFrom(trueValue);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(trueValueAsString);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(falseValue);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(falseValueAsString);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.HasValue.Should().BeFalse();
    }
}
