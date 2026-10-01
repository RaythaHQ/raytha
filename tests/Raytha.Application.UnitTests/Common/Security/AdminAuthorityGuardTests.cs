using FluentAssertions;
using Raytha.Application.Common.Security;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Common.Security;

public class AdminAuthorityGuardTests
{
    private static readonly Guid Posts = Guid.NewGuid();
    private static readonly SystemPermissions All = BuiltInSystemPermission.AllPermissionsAsEnum;
    private static readonly SystemPermissions PeopleOps =
        SystemPermissions.ManageAdministrators | SystemPermissions.ManageSystemSettings;

    private static readonly RoleGrant SuperAdminRole = Role("super_admin", All);
    private static readonly RoleGrant AdminRole = Role("admin", All);
    private static readonly RoleGrant EditorRole = Role(
        "editor",
        SystemPermissions.ManageSitePages,
        (Posts, BuiltInContentTypePermission.AllPermissionsAsEnum)
    );
    private static readonly RoleGrant PeopleOpsRole = Role("people_ops", PeopleOps);

    private static RoleGrant Role(
        string developerName,
        SystemPermissions system,
        params (Guid Id, ContentTypePermissions Permissions)[] contentTypes
    ) =>
        new(
            Guid.NewGuid(),
            developerName.Replace('_', ' '),
            developerName,
            new PermissionGrant(
                system,
                contentTypes.Select(p => new KeyValuePair<Guid, ContentTypePermissions>(
                    p.Id,
                    p.Permissions
                ))
            )
        );

    private static AdminAccount Account(params RoleGrant[] roles) => new(Guid.NewGuid(), true, roles);

    private static PermissionGrant Grant(SystemPermissions system) => new(system, []);

    [Test]
    public void Anonymous_callers_are_denied_everything()
    {
        var target = Account(EditorRole);

        AdminAuthorityGuard
            .CheckAccountAction(null, target, AdminAccountAction.Edit, 1)
            .Should()
            .Be(AdminAuthorityGuard.NotSignedIn);
        AdminAuthorityGuard
            .CheckRoleAssignment(null, null, [EditorRole], 1)
            .Should()
            .Be(AdminAuthorityGuard.NotSignedIn);
        AdminAuthorityGuard
            .CheckRoleDefinition(null, null, PermissionGrant.None)
            .Should()
            .Be(AdminAuthorityGuard.NotSignedIn);
        AdminAuthorityGuard
            .CheckRoleDeletion(null, PeopleOpsRole)
            .Should()
            .Be(AdminAuthorityGuard.NotSignedIn);
    }

    [TestCase(AdminAccountAction.Edit)]
    [TestCase(AdminAccountAction.Suspend)]
    [TestCase(AdminAccountAction.Restore)]
    [TestCase(AdminAccountAction.Delete)]
    [TestCase(AdminAccountAction.ResetPassword)]
    [TestCase(AdminAccountAction.RemoveAccess)]
    [TestCase(AdminAccountAction.ManageApiKeys)]
    public void Only_a_super_admin_may_act_on_a_super_admin_account(AdminAccountAction action)
    {
        var target = Account(SuperAdminRole, AdminRole);

        AdminAuthorityGuard
            .CheckAccountAction(Account(AdminRole), target, action, 2)
            .Should()
            .Be(AdminAuthorityGuard.SuperAdminAccountsOnly);
        AdminAuthorityGuard
            .CheckAccountAction(Account(SuperAdminRole), target, action, 2)
            .Should()
            .BeNull();
    }

    [Test]
    public void Only_a_super_admin_may_assign_the_super_admin_role()
    {
        var target = Account(EditorRole);

        AdminAuthorityGuard
            .CheckRoleAssignment(Account(AdminRole), target, [EditorRole, SuperAdminRole], 1)
            .Should()
            .Be(AdminAuthorityGuard.SuperAdminRoleOnly);
        AdminAuthorityGuard
            .CheckRoleAssignment(Account(AdminRole), null, [SuperAdminRole], 1)
            .Should()
            .Be(AdminAuthorityGuard.SuperAdminRoleOnly);
        AdminAuthorityGuard
            .CheckRoleAssignment(Account(SuperAdminRole), target, [EditorRole, SuperAdminRole], 1)
            .Should()
            .BeNull();
    }

    [Test]
    public void Only_a_super_admin_may_remove_the_super_admin_role()
    {
        var target = Account(SuperAdminRole, EditorRole);

        AdminAuthorityGuard
            .CheckRoleAssignment(Account(AdminRole), target, [EditorRole], 2)
            .Should()
            .Be(AdminAuthorityGuard.SuperAdminRoleOnly);
        AdminAuthorityGuard
            .CheckRoleAssignment(Account(SuperAdminRole), target, [EditorRole], 2)
            .Should()
            .BeNull();
    }

    [Test]
    public void A_role_cannot_grant_more_than_the_caller_holds()
    {
        var caller = Account(PeopleOpsRole);

        AdminAuthorityGuard
            .CheckRoleDefinition(caller, null, Grant(All))
            .Should()
            .StartWith("You can only grant permissions you have yourself.")
            .And.Contain("Manage Templates")
            .And.NotContain("Manage Administrators");
        AdminAuthorityGuard.CheckRoleDefinition(caller, null, Grant(PeopleOps)).Should().BeNull();
    }

    [Test]
    public void A_role_cannot_grant_content_type_access_the_caller_lacks()
    {
        var caller = Account(PeopleOpsRole);
        var request = new PermissionGrant(
            SystemPermissions.None,
            [new(Posts, ContentTypePermissions.Read)]
        );

        AdminAuthorityGuard
            .CheckRoleDefinition(caller, null, request)
            .Should()
            .Contain("content type access you do not hold");
        AdminAuthorityGuard.CheckRoleDefinition(Account(EditorRole), null, request).Should().BeNull();
    }

    [Test]
    public void A_role_above_the_caller_cannot_be_edited_even_to_shrink_it()
    {
        AdminAuthorityGuard
            .CheckRoleDefinition(Account(PeopleOpsRole), AdminRole, Grant(PeopleOps))
            .Should()
            .Be("You cannot edit the admin role because it grants permissions you do not have.");
    }

    [Test]
    public void A_role_above_the_caller_cannot_be_deleted()
    {
        AdminAuthorityGuard
            .CheckRoleDeletion(Account(PeopleOpsRole), EditorRole)
            .Should()
            .Be("You cannot delete the editor role because it grants permissions you do not have.");
        AdminAuthorityGuard.CheckRoleDeletion(Account(AdminRole), EditorRole).Should().BeNull();
    }

    [Test]
    public void A_super_admin_has_no_grant_ceiling()
    {
        var caller = Account(SuperAdminRole);

        AdminAuthorityGuard.CheckRoleDefinition(caller, AdminRole, Grant(All)).Should().BeNull();
        AdminAuthorityGuard
            .CheckRoleAssignment(caller, Account(EditorRole), [AdminRole], 1)
            .Should()
            .BeNull();
    }

    [Test]
    public void Only_roles_within_the_caller_may_be_assigned()
    {
        var caller = Account(PeopleOpsRole);

        AdminAuthorityGuard
            .CheckRoleAssignment(caller, null, [AdminRole], 1)
            .Should()
            .Be("You cannot assign the admin role because it grants permissions you do not have.");
        AdminAuthorityGuard.CheckRoleAssignment(caller, null, [PeopleOpsRole], 1).Should().BeNull();
    }

    [Test]
    public void Keeping_a_role_already_held_is_not_an_assignment()
    {
        var target = Account(PeopleOpsRole);

        AdminAuthorityGuard
            .CheckRoleAssignment(Account(PeopleOpsRole), target, [PeopleOpsRole], 1)
            .Should()
            .BeNull();
    }

    [TestCase(AdminAccountAction.Edit)]
    [TestCase(AdminAccountAction.Suspend)]
    [TestCase(AdminAccountAction.Delete)]
    [TestCase(AdminAccountAction.ResetPassword)]
    [TestCase(AdminAccountAction.ManageApiKeys)]
    public void An_account_with_more_permissions_than_the_caller_cannot_be_managed(
        AdminAccountAction action
    )
    {
        var caller = Account(PeopleOpsRole);

        AdminAuthorityGuard
            .CheckAccountAction(caller, Account(AdminRole), action, 1)
            .Should()
            .Be(AdminAuthorityGuard.AccountAboveCaller);
        AdminAuthorityGuard
            .CheckAccountAction(caller, Account(PeopleOpsRole), action, 1)
            .Should()
            .BeNull();
    }

    [Test]
    public void Nobody_can_change_their_own_roles()
    {
        foreach (var role in new[] { SuperAdminRole, AdminRole })
        {
            var self = Account(role);

            AdminAuthorityGuard
                .CheckRoleAssignment(self, self, [role, EditorRole], 2)
                .Should()
                .Be(AdminAuthorityGuard.OwnRoles);
            AdminAuthorityGuard
                .CheckRoleAssignment(self, self, [EditorRole], 2)
                .Should()
                .Be(AdminAuthorityGuard.OwnRoles);
        }
    }

    [Test]
    public void Editing_your_own_name_with_unchanged_roles_is_allowed()
    {
        var self = Account(AdminRole);

        AdminAuthorityGuard
            .CheckAccountAction(self, self, AdminAccountAction.Edit, 1)
            .Should()
            .BeNull();
        AdminAuthorityGuard.CheckRoleAssignment(self, self, [AdminRole], 1).Should().BeNull();
    }

    [TestCase(AdminAccountAction.Delete, "You cannot remove your own account.")]
    [TestCase(AdminAccountAction.Suspend, "You cannot change the status on your own account.")]
    [TestCase(AdminAccountAction.Restore, "You cannot change the status on your own account.")]
    [TestCase(AdminAccountAction.RemoveAccess, "You cannot remove your own admin access.")]
    public void Nobody_can_lock_themselves_out(AdminAccountAction action, string message)
    {
        var self = Account(SuperAdminRole);

        AdminAuthorityGuard.CheckAccountAction(self, self, action, 2).Should().Be(message);
    }

    [TestCase(AdminAccountAction.ResetPassword)]
    [TestCase(AdminAccountAction.ManageApiKeys)]
    public void You_may_manage_your_own_credentials(AdminAccountAction action)
    {
        var self = Account(PeopleOpsRole);

        AdminAuthorityGuard.CheckAccountAction(self, self, action, 1).Should().BeNull();
    }

    [TestCase(AdminAccountAction.Delete, "The last active super admin cannot be deleted.")]
    [TestCase(AdminAccountAction.Suspend, "The last active super admin cannot be suspended.")]
    [TestCase(
        AdminAccountAction.RemoveAccess,
        "The last active super admin cannot have admin access removed."
    )]
    public void The_last_active_super_admin_cannot_be_deactivated(
        AdminAccountAction action,
        string message
    )
    {
        var caller = Account(SuperAdminRole);
        var target = Account(SuperAdminRole);

        AdminAuthorityGuard.CheckAccountAction(caller, target, action, 1).Should().Be(message);
        AdminAuthorityGuard.CheckAccountAction(caller, target, action, 2).Should().BeNull();
    }

    [Test]
    public void The_last_active_super_admin_can_still_be_edited_and_reset()
    {
        var caller = Account(SuperAdminRole);
        var target = Account(SuperAdminRole);

        AdminAuthorityGuard
            .CheckAccountAction(caller, target, AdminAccountAction.Edit, 1)
            .Should()
            .BeNull();
        AdminAuthorityGuard
            .CheckAccountAction(caller, target, AdminAccountAction.ResetPassword, 1)
            .Should()
            .BeNull();
    }

    [Test]
    public void A_suspended_super_admin_is_not_the_last_active_one()
    {
        var suspended = new AdminAccount(Guid.NewGuid(), false, [SuperAdminRole]);

        AdminAuthorityGuard
            .CheckAccountAction(Account(SuperAdminRole), suspended, AdminAccountAction.Delete, 1)
            .Should()
            .BeNull();
    }

    [Test]
    public void The_last_active_super_admin_cannot_lose_the_role()
    {
        var target = Account(SuperAdminRole, EditorRole);

        AdminAuthorityGuard
            .CheckRoleAssignment(Account(SuperAdminRole), target, [EditorRole], 1)
            .Should()
            .Be(AdminAuthorityGuard.LastSuperAdminRole);
    }

    [Test]
    public void The_super_admin_role_cannot_be_edited_or_deleted_by_anyone()
    {
        var caller = Account(SuperAdminRole);

        AdminAuthorityGuard
            .CheckRoleDefinition(caller, SuperAdminRole, Grant(All))
            .Should()
            .Be(AdminAuthorityGuard.SuperAdminRoleLocked);
        AdminAuthorityGuard
            .CheckRoleDeletion(caller, SuperAdminRole)
            .Should()
            .Be(AdminAuthorityGuard.SuperAdminRoleUndeletable);
    }

    [Test]
    public void Built_in_admin_and_editor_roles_stay_editable()
    {
        var caller = Account(SuperAdminRole);

        AdminAuthorityGuard.CheckRoleDefinition(caller, AdminRole, Grant(PeopleOps)).Should().BeNull();
        AdminAuthorityGuard
            .CheckRoleDefinition(caller, EditorRole, Grant(SystemPermissions.ManageSitePages))
            .Should()
            .BeNull();
    }

    [Test]
    public void Full_trust_counts_as_every_permission()
    {
        var caller = Account(Role("settings", SystemPermissions.ManageSystemSettings));

        AdminAuthorityGuard.CheckRoleDefinition(caller, null, Grant(All)).Should().BeNull();
        AdminAuthorityGuard
            .CheckRoleDefinition(
                Account(Role("clerk", SystemPermissions.ManageUsers)),
                null,
                Grant(SystemPermissions.ManageSystemSettings)
            )
            .Should()
            .NotBeNull();
    }

    [Test]
    public void Built_in_roles_grant_the_media_library()
    {
        BuiltInRole.SuperAdmin.DefaultSystemPermission.Should().HaveFlag(SystemPermissions.ManageMediaItems);
        BuiltInRole.Admin.DefaultSystemPermission.Should().HaveFlag(SystemPermissions.ManageMediaItems);
    }
}
