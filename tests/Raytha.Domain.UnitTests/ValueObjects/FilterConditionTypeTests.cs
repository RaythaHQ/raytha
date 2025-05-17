using FluentAssertions;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class FilterConditionTypeTests
{
    [Test]
    [TestCase("filter_condition")]
    [TestCase("filter_condition_group")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = FilterConditionType.From(developerName);
        type.DeveloperName.Should().Be(developerName.ToLower());
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = FilterConditionType.FilterCondition;

        type.Should().Be("filter_condition");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (FilterConditionType)"filter_condition";

        type.Should().Be(FilterConditionType.FilterCondition);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => FilterConditionType.From("BadValue"))
            .Should()
            .Throw<FilterConditionTypeNotFoundException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        FilterConditionType.SupportedTypes.Count().Should().Be(2);
    }
}
