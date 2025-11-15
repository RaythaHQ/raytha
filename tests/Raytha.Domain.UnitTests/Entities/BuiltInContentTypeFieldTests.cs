using FluentAssertions;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class BuiltInContentTypeFieldTests
{
    [Test]
    [TestCase("PrimaryField")]
    [TestCase("CreationTime")]
    [TestCase("CreatorUser")]
    [TestCase("LastModificationTime")]
    [TestCase("LastModifierUser")]
    [TestCase("Id")]
    [TestCase("IsDraft")]
    [TestCase("IsPublished")]
    [TestCase("Template")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = BuiltInContentTypeField.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    [TestCase("PrimaryField", "Primary field")]
    [TestCase("CreationTime", "Created at")]
    [TestCase("CreatorUser", "Created by")]
    [TestCase("LastModificationTime", "Last modified at")]
    [TestCase("LastModifierUser", "Last modified by")]
    [TestCase("Id", "Id")]
    [TestCase("IsDraft", "Is draft")]
    [TestCase("IsPublished", "Is published")]
    [TestCase("Template", "Template")]
    [Parallelizable(ParallelScope.All)]
    public void ToStringShouldMatchLabel(string developerName, string label)
    {
        var type = BuiltInContentTypeField.From(developerName);
        type.ToString().Should().Be(label);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = BuiltInContentTypeField.PrimaryField;

        type.Should().Be("PrimaryField");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (BuiltInContentTypeField)"PrimaryField";

        type.Should().Be(BuiltInContentTypeField.PrimaryField);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => BuiltInContentTypeField.From("BadValue"))
            .Should()
            .Throw<ReservedContentTypeFieldNotFoundException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        BuiltInContentTypeField.ReservedContentTypeFields.Count().Should().Be(9);
    }
}
