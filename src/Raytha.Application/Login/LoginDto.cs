using System.Linq.Expressions;
using Raytha.Application.Common.Models;
using Raytha.Application.Roles;
using Raytha.Application.UserGroups;
using Raytha.Domain.Entities;

namespace Raytha.Application.Login;

public record LoginDto : BaseEntityDto
{
    public string EmailAddress { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName
    {
        get { return $"{FirstName} {LastName}"; }
    }

    public DateTime? LastModificationTime { get; init; }
    public DateTime? LastLoggedInTime { get; init; }
    public string AuthenticationScheme { get; init; } = string.Empty;
    public string SsoId { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool IsActive { get; init; }

    public IEnumerable<RoleDto> Roles { get; init; } = new List<RoleDto>();
    public IEnumerable<UserGroupDto> UserGroups { get; init; } = new List<UserGroupDto>();
    public dynamic CustomAttributes { get; init; }

    public static Expression<Func<User, LoginDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static LoginDto GetProjection(User entity)
    {
        return new LoginDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            EmailAddress = entity.EmailAddress,
            LastLoggedInTime = entity.LastLoggedInTime,
            IsActive = entity.IsActive,
            LastModificationTime = entity.LastModificationTime,
            Roles = entity.Roles.AsQueryable().Select(RoleDto.GetProjection()),
            UserGroups = entity.UserGroups.AsQueryable().Select(UserGroupDto.GetProjection()),
            IsAdmin = entity.IsAdmin,
            SsoId = entity.SsoId,
            AuthenticationScheme = entity.AuthenticationScheme?.DeveloperName ?? string.Empty,
        };
    }
}
