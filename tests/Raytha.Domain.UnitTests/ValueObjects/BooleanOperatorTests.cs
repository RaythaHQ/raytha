using FluentAssertions;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class BooleanOperatorTests
{
    [Test]
    [TestCase("AND")]
    [TestCase("OR")]
    [TestCase("NOT")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = BooleanOperator.From(developerName);
        type.DeveloperName.Should().Be(developerName.ToLower());
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = BooleanOperator.AND;

        type.Should().Be("and");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (BooleanOperator)"and";

        type.Should().Be(BooleanOperator.AND);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => BooleanOperator.From("BadValue"))
            .Should()
            .Throw<UnsupportedBooleanOperatorException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        BooleanOperator.SupportedOperators.Count().Should().Be(3);
    }
}
