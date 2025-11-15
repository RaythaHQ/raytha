using FluentAssertions;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class GuidFieldValueTests
{
    private Guid? guidValue = Guid.Parse("b0c917f6-9614-44b8-94c9-b92a4165c3cf");
    private string guidValueAsString = Guid.Parse("b0c917f6-9614-44b8-94c9-b92a4165c3cf")
        .ToString();

    [Test]
    public void ShouldReturnCorrectFieldValue()
    {
        var type = BaseFieldType.From("id");
        var fieldValue = type.FieldValueFrom(guidValue);
        fieldValue.Should().BeOfType<GuidFieldValue>();
        ((Guid?)fieldValue.Value).Should().Be(guidValue);

        fieldValue = type.FieldValueFrom(guidValueAsString);
        fieldValue.Should().BeOfType<GuidFieldValue>();
        ((Guid?)fieldValue.Value).Should().Be(guidValue);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Should().BeOfType<GuidFieldValue>();
        ((Guid?)fieldValue.Value).Should().Be(Guid.Empty);
    }

    [Test]
    public void ShouldTextMatch()
    {
        var type = BaseFieldType.From("id");
        var fieldValue = type.FieldValueFrom(guidValue);
        fieldValue.Text.Should().Be(guidValueAsString);

        fieldValue = type.FieldValueFrom(guidValueAsString);
        fieldValue.Text.Should().Be(guidValueAsString);

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.Text.Should().Be(string.Empty);
    }

    [Test]
    public void ShouldHaveCorrectHasValue()
    {
        var type = BaseFieldType.From("id");
        var fieldValue = type.FieldValueFrom(guidValue);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(guidValueAsString);
        fieldValue.HasValue.Should().BeTrue();

        fieldValue = type.FieldValueFrom(string.Empty);
        fieldValue.HasValue.Should().BeFalse();
    }
}
