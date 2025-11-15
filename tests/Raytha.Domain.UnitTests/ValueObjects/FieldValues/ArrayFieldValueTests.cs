using FluentAssertions;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class ArrayFieldValueTests
{
    private string[] twoValueArray = new string[] { "developer_name_1", "developer_name_2" };
    private string[] emptyArray = new string[0];

    private string twoValueArrayAsString = @"[""developer_name_1"", ""developer_name_2""]";
    private string emptyArrayAsString = "[]";

    [Test]
    public void ShouldReturnCorrectFieldValue()
    {
        var type = BaseFieldType.From("multiple_select");
        var fieldValue = type.FieldValueFrom(twoValueArray);
        fieldValue.Should().BeOfType<ArrayFieldValue>();
        ((string[])fieldValue.Value).Should().BeEquivalentTo(twoValueArray);

        fieldValue = type.FieldValueFrom(twoValueArrayAsString);
        fieldValue.Should().BeOfType<ArrayFieldValue>();
        ((string[])fieldValue.Value).Should().BeEquivalentTo(twoValueArray);

        fieldValue = type.FieldValueFrom(emptyArray);
        fieldValue.Should().BeOfType<ArrayFieldValue>();
        ((string[])fieldValue.Value).Should().BeEquivalentTo(emptyArray);

        fieldValue = type.FieldValueFrom(emptyArrayAsString);
        fieldValue.Should().BeOfType<ArrayFieldValue>();
        ((string[])fieldValue.Value).Should().BeEquivalentTo(emptyArray);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Should().BeOfType<ArrayFieldValue>();
        ((string[])fieldValue.Value).Should().BeNull();
    }

    [Test]
    public void ShouldTextMatch()
    {
        var type = BaseFieldType.From("multiple_select");
        var fieldValue = type.FieldValueFrom(twoValueArray);
        fieldValue.Text.Should().Be(string.Join(", ", twoValueArray));

        fieldValue = type.FieldValueFrom(twoValueArrayAsString);
        fieldValue.Text.Should().Be(string.Join(", ", twoValueArray));

        fieldValue = type.FieldValueFrom(emptyArray);
        fieldValue.Text.Should().Be(string.Join(", ", emptyArray));

        fieldValue = type.FieldValueFrom(emptyArrayAsString);
        fieldValue.Text.Should().Be(string.Join(", ", emptyArray));

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Text.Should().Be(string.Join(", ", emptyArray));
    }

    [Test]
    public void ShouldHaveCorrectHasValue()
    {
        var type = BaseFieldType.From("multiple_select");
        var fieldValue = type.FieldValueFrom(emptyArray);
        fieldValue.HasValue.Should().BeFalse();

        fieldValue = type.FieldValueFrom(emptyArrayAsString);
        fieldValue.HasValue.Should().BeFalse();

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.HasValue.Should().BeFalse();

        fieldValue = type.FieldValueFrom(twoValueArray);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(twoValueArrayAsString);
        fieldValue.HasValue.Should().BeTrue();
    }
}
