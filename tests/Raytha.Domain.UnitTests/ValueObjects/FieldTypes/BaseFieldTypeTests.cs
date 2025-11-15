using FluentAssertions;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class BaseFieldTypeTests
{
    [Test]
    [TestCase("single_line_text")]
    [TestCase("long_text")]
    [TestCase("wysiwyg")]
    [TestCase("radio")]
    [TestCase("dropdown")]
    [TestCase("checkbox")]
    [TestCase("multiple_select")]
    [TestCase("date")]
    [TestCase("number")]
    [TestCase("attachment")]
    [TestCase("one_to_one_relationship")]
    [TestCase("id")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = BaseFieldType.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    [TestCase("single_line_text", "Single line text")]
    [TestCase("long_text", "Long text")]
    [TestCase("wysiwyg", "Wysiwyg")]
    [TestCase("radio", "Radio")]
    [TestCase("dropdown", "Dropdown")]
    [TestCase("checkbox", "Checkbox")]
    [TestCase("multiple_select", "Multiple select")]
    [TestCase("date", "Date")]
    [TestCase("number", "Number")]
    [TestCase("attachment", "Attachment")]
    [TestCase("one_to_one_relationship", "One to one relationship")]
    [TestCase("id", "Id")]
    [Parallelizable(ParallelScope.All)]
    public void ToStringShouldMatchLabel(string developerName, string label)
    {
        var type = BaseFieldType.From(developerName);
        type.ToString().Should().Be(label);
    }

    [Test]
    [TestCase("single_line_text", false)]
    [TestCase("long_text", false)]
    [TestCase("wysiwyg", false)]
    [TestCase("radio", true)]
    [TestCase("dropdown", true)]
    [TestCase("checkbox", false)]
    [TestCase("multiple_select", true)]
    [TestCase("date", false)]
    [TestCase("number", false)]
    [TestCase("attachment", false)]
    [TestCase("one_to_one_relationship", false)]
    [TestCase("id", false)]
    [Parallelizable(ParallelScope.All)]
    public void ShouldHaveChoicesOrNot(string developerName, bool hasChoices)
    {
        var type = BaseFieldType.From(developerName);
        type.HasChoices.Should().Be(hasChoices);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = BaseFieldType.SingleLineText;

        type.Should().Be("single_line_text");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (BaseFieldType)"single_line_text";

        type.Should().Be(BaseFieldType.SingleLineText);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => BaseFieldType.From("BadValue"))
            .Should()
            .Throw<UnsupportedFieldTypeException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        BaseFieldType.SupportedTypes.Count().Should().Be(11);
    }

    [Test]
    [TestCase("single_line_text")]
    [TestCase("long_text")]
    [TestCase("wysiwyg")]
    [TestCase("one_to_one_relationship")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldMatchTextFieldType(string developerName)
    {
        BaseFieldType type = BaseFieldType.From(developerName);
        type.Should().BeAssignableTo<TextFieldType>();
        type.SupportedConditionOperators.Count().Should().Be(10);
    }

    [Test]
    [TestCase("radio")]
    [TestCase("dropdown")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldMatchSingleSelectFieldType(string developerName)
    {
        BaseFieldType type = BaseFieldType.From(developerName);
        type.Should().BeAssignableTo<SingleSelectFieldType>();
        type.SupportedConditionOperators.Count().Should().Be(4);
    }

    [Test]
    [TestCase("multiple_select")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldMatchMultipleSelectFieldType(string developerName)
    {
        BaseFieldType type = BaseFieldType.From(developerName);
        type.Should().BeOfType<MultipleSelectFieldType>();
        type.SupportedConditionOperators.Count().Should().Be(4);
    }

    [Test]
    [TestCase("number")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldMatchNumericValueFieldType(string developerName)
    {
        BaseFieldType type = BaseFieldType.From(developerName);
        type.Should().BeAssignableTo<NumericValueFieldType>();
        type.SupportedConditionOperators.Count().Should().Be(8);
    }

    [Test]
    [TestCase("date")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldMatchDateFieldType(string developerName)
    {
        BaseFieldType type = BaseFieldType.From(developerName);
        type.Should().BeOfType<DateFieldType>();
        type.SupportedConditionOperators.Count().Should().Be(8);
    }

    [Test]
    [TestCase("id")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldMatchEqualsOrNotEqualsFieldType(string developerName)
    {
        BaseFieldType type = BaseFieldType.From(developerName);
        type.Should().BeOfType<IdentifierFieldType>();
        type.SupportedConditionOperators.Count().Should().Be(2);
    }

    [Test]
    [TestCase("attachment")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldMatchAttachmentFieldType(string developerName)
    {
        BaseFieldType type = BaseFieldType.From(developerName);
        type.Should().BeOfType<AttachmentFieldType>();
        type.SupportedConditionOperators.Count().Should().Be(2);
    }

    [Test]
    [TestCase("checkbox")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldMatchCheckboxFieldType(string developerName)
    {
        BaseFieldType type = BaseFieldType.From(developerName);
        type.Should().BeOfType<CheckboxFieldType>();
        type.SupportedConditionOperators.Count().Should().Be(2);
    }
}
