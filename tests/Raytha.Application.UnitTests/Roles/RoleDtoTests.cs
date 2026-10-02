using FluentAssertions;
using Raytha.Application.Roles;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Roles;

public class RoleDtoTests
{
    [Test]
    public void A_role_stored_with_only_system_settings_lists_every_permission()
    {
        var dto = RoleDto.GetProjection(
            new Role
            {
                Id = Guid.NewGuid(),
                Label = "Ops",
                DeveloperName = "ops",
                SystemPermissions = SystemPermissions.ManageSystemSettings,
            }
        );

        dto.SystemPermissions.Should()
            .BeEquivalentTo(
                BuiltInSystemPermission
                    .From(BuiltInSystemPermission.AllPermissionsAsEnum)
                    .Select(p => p.DeveloperName)
            );
    }
}
