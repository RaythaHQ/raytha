using FluentAssertions;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class BuiltInSystemPermissionTests
{
    [Test]
    [TestCase(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
    [TestCase(BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION)]
    [TestCase(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
    [TestCase(BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
    [TestCase(BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
    [TestCase(BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    [TestCase(BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)]
    [TestCase(BuiltInSystemPermission.MANAGE_MEDIA_ITEMS_PERMISSION)]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = BuiltInSystemPermission.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    [TestCase(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION, "Manage System Settings")]
    [TestCase(BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION, "Manage Audit Logs")]
    [TestCase(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION, "Manage Content Types")]
    [TestCase(BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION, "Manage Templates")]
    [TestCase(BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION, "Manage Administrators")]
    [TestCase(BuiltInSystemPermission.MANAGE_USERS_PERMISSION, "Manage Users")]
    [TestCase(BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION, "Manage Site Pages")]
    [TestCase(BuiltInSystemPermission.MANAGE_MEDIA_ITEMS_PERMISSION, "Manage Media")]
    [Parallelizable(ParallelScope.All)]
    public void ToStringShouldMatchLabel(string developerName, string label)
    {
        var type = BuiltInSystemPermission.From(developerName);
        type.Label.Should().Be(label);
    }

    [Test]
    public void ShouldHandleListOfPermissions()
    {
        var typesAsList = BuiltInSystemPermission.From(
            BuiltInSystemPermission.AllPermissionsAsEnum
        );

        typesAsList.Count().Should().Be(BuiltInSystemPermission.Permissions.Count());
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => BuiltInSystemPermission.From("BadValue"))
            .Should()
            .Throw<UnsupportedTemplateTypeException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        BuiltInSystemPermission.Permissions.Count().Should().Be(8);
    }

    [Test]
    public void Full_trust_implies_every_system_permission()
    {
        BuiltInSystemPermission
            .WithImplied(SystemPermissions.ManageSystemSettings)
            .Should()
            .Be(BuiltInSystemPermission.AllPermissionsAsEnum);
        BuiltInSystemPermission
            .WithImplied(SystemPermissions.ManageAdministrators)
            .Should()
            .Be(BuiltInSystemPermission.AllPermissionsAsEnum);
        BuiltInSystemPermission
            .WithImplied(SystemPermissions.ManageTemplates)
            .Should()
            .Be(SystemPermissions.ManageTemplates);
        BuiltInSystemPermission.WithImplied(SystemPermissions.None).Should().Be(SystemPermissions.None);
        BuiltInSystemPermission
            .WithImplied([BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION])
            .Should()
            .BeEquivalentTo(
                "system_settings",
                "administrators",
                "audit_logs",
                "content_types",
                "templates",
                "users",
                "site_pages",
                "media_items"
            );
    }
}
