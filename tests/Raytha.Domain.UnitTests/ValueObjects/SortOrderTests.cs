using FluentAssertions;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class SortOrderTests
{
    [Test]
    [TestCase(SortOrder.ASCENDING)]
    [TestCase(SortOrder.DESCENDING)]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = SortOrder.From(developerName);
        type.DeveloperName.Should().Be(developerName.ToLower());
    }

    [Test]
    [TestCase(SortOrder.ASCENDING, "Ascending")]
    [TestCase(SortOrder.DESCENDING, "Descending")]
    [Parallelizable(ParallelScope.All)]
    public void ToStringShouldMatchLabel(string developerName, string label)
    {
        var type = SortOrder.From(developerName);
        type.Label.Should().Be(label);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = SortOrder.Ascending;

        type.Should().Be(SortOrder.ASCENDING);
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (SortOrder)SortOrder.ASCENDING;

        type.Should().Be(SortOrder.Ascending);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions.Invoking(() => SortOrder.From("BadValue"))
            .Should().Throw<SortOrderNotFoundException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        SortOrder.SupportedTypes.Count().Should().Be(2);
    }
}
