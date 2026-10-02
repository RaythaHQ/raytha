using FluentAssertions;
using Raytha.Application.Common.Security;
using Raytha.Application.Login;
using Raytha.Application.Roles;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Common.Security;

public class ImpersonationRulesTests
{
    private static ImpersonationParty Party(
        bool isActive = true,
        bool isAdmin = false,
        bool isSuperAdmin = false,
        bool canManageUsers = false
    ) => new(Guid.NewGuid(), isActive, isAdmin, isSuperAdmin, canManageUsers);

    private static readonly ImpersonationParty SuperAdmin = Party(
        isAdmin: true,
        isSuperAdmin: true,
        canManageUsers: true
    );
    private static readonly ImpersonationParty UserManager = Party(isAdmin: true, canManageUsers: true);
    private static readonly ImpersonationParty WebsiteUser = Party();
    private static readonly ImpersonationParty OtherAdmin = Party(isAdmin: true, canManageUsers: true);

    [Test]
    public void An_anonymous_caller_is_denied()
    {
        ImpersonationRules
            .Check(null, WebsiteUser, callerIsImpersonating: false)
            .Should()
            .Be(ImpersonationRules.NotSignedIn);
    }

    [Test]
    public void A_caller_already_impersonating_cannot_start_another_session()
    {
        ImpersonationRules
            .Check(SuperAdmin, WebsiteUser, callerIsImpersonating: true)
            .Should()
            .Be(ImpersonationRules.AlreadyImpersonating);
    }

    [Test]
    public void Nobody_can_impersonate_themselves()
    {
        ImpersonationRules
            .Check(SuperAdmin, SuperAdmin, callerIsImpersonating: false)
            .Should()
            .Be(ImpersonationRules.Self);
    }

    [Test]
    public void A_suspended_target_cannot_be_impersonated()
    {
        ImpersonationRules
            .Check(SuperAdmin, Party(isActive: false), callerIsImpersonating: false)
            .Should()
            .Be(ImpersonationRules.TargetInactive);
    }

    [Test]
    public void A_website_user_caller_is_denied()
    {
        ImpersonationRules
            .Check(Party(canManageUsers: true), WebsiteUser, callerIsImpersonating: false)
            .Should()
            .Be(ImpersonationRules.CallerNotAdmin);
    }

    [Test]
    public void A_suspended_admin_caller_is_denied()
    {
        var suspended = Party(isActive: false, isAdmin: true, isSuperAdmin: true, canManageUsers: true);

        ImpersonationRules
            .Check(suspended, WebsiteUser, callerIsImpersonating: false)
            .Should()
            .Be(ImpersonationRules.CallerNotAdmin);
    }

    [Test]
    public void An_admin_who_is_not_a_super_admin_cannot_impersonate_an_admin()
    {
        ImpersonationRules
            .Check(UserManager, OtherAdmin, callerIsImpersonating: false)
            .Should()
            .Be(ImpersonationRules.SuperAdminOnly);
    }

    [Test]
    public void An_admin_without_the_users_permission_cannot_impersonate_a_website_user()
    {
        ImpersonationRules
            .Check(Party(isAdmin: true), WebsiteUser, callerIsImpersonating: false)
            .Should()
            .Be(ImpersonationRules.UsersPermissionRequired);
    }

    [Test]
    public void An_admin_with_the_users_permission_can_impersonate_a_website_user()
    {
        ImpersonationRules.Check(UserManager, WebsiteUser, callerIsImpersonating: false).Should().BeNull();
    }

    [Test]
    public void A_super_admin_can_impersonate_an_admin_and_a_website_user()
    {
        ImpersonationRules.Check(SuperAdmin, OtherAdmin, callerIsImpersonating: false).Should().BeNull();
        ImpersonationRules.Check(SuperAdmin, WebsiteUser, callerIsImpersonating: false).Should().BeNull();
    }

    [Test]
    public void A_party_is_super_admin_by_role_and_manages_users_by_permission()
    {
        static LoginDto Login(string role, params string[] permissions) =>
            new()
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                IsAdmin = true,
                Roles = [new RoleDto { DeveloperName = role, SystemPermissions = permissions }],
            };

        var superAdmin = ImpersonationParty.Of(
            Login(BuiltInRole.SuperAdmin.DeveloperName, BuiltInSystemPermission.MANAGE_USERS_PERMISSION)
        );
        var fullAdmin = ImpersonationParty.Of(
            Login(BuiltInRole.Admin.DeveloperName, BuiltInSystemPermission.MANAGE_USERS_PERMISSION)
        );
        var editor = ImpersonationParty.Of(
            Login(BuiltInRole.Editor.DeveloperName, BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)
        );

        superAdmin.Should().Match<ImpersonationParty>(p => p.IsSuperAdmin && p.CanManageUsers);
        fullAdmin.Should().Match<ImpersonationParty>(p => !p.IsSuperAdmin && p.CanManageUsers);
        editor.Should().Match<ImpersonationParty>(p => !p.IsSuperAdmin && !p.CanManageUsers);
    }
}
