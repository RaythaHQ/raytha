using FluentAssertions;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class BuiltInContentTypePermissionTests
{
    [Test]
    [TestCase("read")]
    [TestCase("edit")]
    [TestCase("config")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = BuiltInContentTypePermission.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    [TestCase("read", "Read")]
    [TestCase("edit", "Edit")]
    [TestCase("config", "Config")]
    [Parallelizable(ParallelScope.All)]
    public void ToStringShouldMatchLabel(string developerName, string label)
    {
        var type = BuiltInContentTypePermission.From(developerName);
        type.Label.Should().Be(label);
    }


    [Test]
    public void ShouldHandleListOfPermissions()
    {
        var typesAsList = BuiltInContentTypePermission.From(BuiltInContentTypePermission.AllPermissionsAsEnum);

        typesAsList.Count().Should().Be(BuiltInContentTypePermission.Permissions.Count());
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions.Invoking(() => BuiltInContentTypePermission.From("BadValue"))
            .Should().Throw<UnsupportedContentTypePermissionException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        BuiltInContentTypePermission.Permissions.Count().Should().Be(3);
    }
}
