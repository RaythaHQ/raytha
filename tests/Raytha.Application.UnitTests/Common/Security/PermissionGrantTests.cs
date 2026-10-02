using CSharpVitamins;
using FluentAssertions;
using Raytha.Application.Common.Security;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Common.Security;

public class PermissionGrantTests
{
    private static readonly Guid Posts = Guid.NewGuid();
    private static readonly Guid Pages = Guid.NewGuid();

    private static PermissionGrant Grant(
        SystemPermissions system,
        params (Guid Id, ContentTypePermissions Permissions)[] contentTypes
    ) =>
        new(
            system,
            contentTypes.Select(p => new KeyValuePair<Guid, ContentTypePermissions>(p.Id, p.Permissions))
        );

    [TestCase(ContentTypePermissions.Edit)]
    [TestCase(ContentTypePermissions.Config)]
    [TestCase(ContentTypePermissions.Edit | ContentTypePermissions.Config)]
    public void Edit_or_config_implies_read(ContentTypePermissions permissions)
    {
        Grant(SystemPermissions.None, (Posts, permissions))
            .ContentTypes[Posts]
            .Should()
            .HaveFlag(ContentTypePermissions.Read);
    }

    [Test]
    public void Read_alone_implies_nothing_else()
    {
        Grant(SystemPermissions.None, (Posts, ContentTypePermissions.Read))
            .ContentTypes[Posts]
            .Should()
            .Be(ContentTypePermissions.Read);
    }

    [Test]
    public void Content_types_with_no_permission_are_dropped()
    {
        Grant(SystemPermissions.None, (Posts, ContentTypePermissions.None))
            .ContentTypes.Should()
            .BeEmpty();
    }

    [Test]
    public void From_request_normalizes_what_a_role_will_save()
    {
        var grant = PermissionGrant.FromRequest(
            [BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION],
            new Dictionary<string, IEnumerable<string>>
            {
                [new ShortGuid(Posts).ToString()] = [
                    BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION,
                ],
            }
        );

        grant.System.Should().Be(SystemPermissions.ManageTemplates);
        grant
            .ToContentTypeRolePermissions()
            .Should()
            .ContainSingle(p =>
                p.ContentTypeId == Posts
                && p.ContentTypePermissions
                    == (ContentTypePermissions.Read | ContentTypePermissions.Edit)
            );
    }

    [Test]
    public void From_request_tolerates_missing_collections()
    {
        PermissionGrant.FromRequest(null, null).IsWithin(PermissionGrant.None).Should().BeTrue();
    }

    [Test]
    public void System_settings_or_administrators_saves_every_system_permission()
    {
        var settings = PermissionGrant.FromRequest(
            [BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION],
            null
        );
        var administrators = PermissionGrant.FromRequest(
            [BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION],
            null
        );

        settings.System.Should().Be(BuiltInSystemPermission.AllPermissionsAsEnum);
        administrators.System.Should().Be(BuiltInSystemPermission.AllPermissionsAsEnum);
        settings.CoversAllContentTypes.Should().BeTrue();
    }

    [Test]
    public void Union_merges_system_and_per_type_permissions()
    {
        var union = PermissionGrant.Union([
            Grant(SystemPermissions.ManageUsers, (Posts, ContentTypePermissions.Read)),
            Grant(SystemPermissions.ManageTemplates, (Posts, ContentTypePermissions.Config)),
        ]);

        union.System.Should().Be(SystemPermissions.ManageUsers | SystemPermissions.ManageTemplates);
        union
            .ContentTypes[Posts]
            .Should()
            .Be(ContentTypePermissions.Read | ContentTypePermissions.Config);
    }

    [Test]
    public void A_grant_is_within_itself_and_anything_larger()
    {
        var small = Grant(SystemPermissions.ManageUsers, (Posts, ContentTypePermissions.Read));
        var large = Grant(
            SystemPermissions.ManageUsers | SystemPermissions.ManageTemplates,
            (Posts, BuiltInContentTypePermission.AllPermissionsAsEnum)
        );

        small.IsWithin(small).Should().BeTrue();
        small.IsWithin(large).Should().BeTrue();
        large.IsWithin(small).Should().BeFalse();
    }

    [Test]
    public void Extra_system_permission_is_beyond_the_ceiling()
    {
        var request = Grant(SystemPermissions.ManageUsers | SystemPermissions.ManageAuditLogs);
        var ceiling = Grant(SystemPermissions.ManageUsers);

        request.IsWithin(ceiling).Should().BeFalse();
        request.SystemBeyond(ceiling).Should().Be(SystemPermissions.ManageAuditLogs);
    }

    [Test]
    public void Access_to_an_unheld_content_type_is_beyond_the_ceiling()
    {
        var request = Grant(SystemPermissions.None, (Pages, ContentTypePermissions.Read));
        var ceiling = Grant(SystemPermissions.None, (Posts, ContentTypePermissions.Read));

        request.IsWithin(ceiling).Should().BeFalse();
        request.HasContentTypesBeyond(ceiling).Should().BeTrue();
    }

    [Test]
    public void Manage_content_types_covers_every_content_type_permission()
    {
        var request = Grant(
            SystemPermissions.None,
            (Posts, BuiltInContentTypePermission.AllPermissionsAsEnum),
            (Pages, ContentTypePermissions.Edit)
        );

        request.IsWithin(Grant(SystemPermissions.ManageContentTypes)).Should().BeTrue();
    }

    [Test]
    public void Granting_manage_content_types_needs_it_held_not_every_type()
    {
        var request = Grant(SystemPermissions.ManageContentTypes);
        var ceiling = Grant(
            SystemPermissions.None,
            (Posts, BuiltInContentTypePermission.AllPermissionsAsEnum)
        );

        request.IsWithin(ceiling).Should().BeFalse();
    }
}
