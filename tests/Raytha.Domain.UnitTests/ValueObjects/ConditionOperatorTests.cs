using FluentAssertions;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class ConditionOperatorTests
{
    [Test]
    [TestCase("lt")]
    [TestCase("le")]
    [TestCase("gt")]
    [TestCase("ge")]
    [TestCase("eq")]
    [TestCase("ne")]
    [TestCase("contains")]
    [TestCase("notcontains")]
    [TestCase("startswith")]
    [TestCase("notstartswith")]
    [TestCase("endswith")]
    [TestCase("notendswith")]
    [TestCase("has")]
    [TestCase("nothas")]
    [TestCase("true")]
    [TestCase("false")]
    [TestCase("empty")]
    [TestCase("notempty")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = ConditionOperator.From(developerName);
        type.DeveloperName.Should().Be(developerName.ToLower());
    }

    [Test]
    [TestCase("lt", "<")]
    [TestCase("le", "≤")]
    [TestCase("gt", ">")]
    [TestCase("ge", "≥")]
    [TestCase("eq", "=")]
    [TestCase("ne", "≠")]
    [TestCase("contains", "contains")]
    [TestCase("notcontains", "does not contain")]
    [TestCase("startswith", "starts with")]
    [TestCase("notstartswith", "does not start with")]
    [TestCase("endswith", "ends with")]
    [TestCase("notendswith", "does not end with")]
    [TestCase("has", "has")]
    [TestCase("nothas", "does not have")]
    [TestCase("true", "is true")]
    [TestCase("false", "is false")]
    [TestCase("empty", "is empty")]
    [TestCase("notempty", "is not empty")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectLabel(string developerName, string label)
    {
        var type = ConditionOperator.From(developerName);
        type.Label.Should().Be(label);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = ConditionOperator.EQUALS;

        type.Should().Be("eq");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (ConditionOperator)"eq";

        type.Should().Be(ConditionOperator.EQUALS);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => ConditionOperator.From("BadValue"))
            .Should()
            .Throw<UnsupportedConditionOperatorException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        ConditionOperator.SupportedOperators.Count().Should().Be(18);
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypesWithoutValues()
    {
        ConditionOperator.OperatorsWithoutValues.Count().Should().Be(4);
    }
}
