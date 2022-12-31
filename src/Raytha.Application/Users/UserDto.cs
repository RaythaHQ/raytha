using Raytha.Application.Common.Models;
using Raytha.Application.UserGroups;
using Raytha.Domain.Entities;
using System.Linq.Expressions;

namespace Raytha.Application.Users
{
    public record UserDto : BaseAuditableEntityDto
    {
        public bool IsActive { get; init; }
        public DateTime? LastLoggedInTime { get; init; }
        public AuditableUserDto CreatorUser { get; init; }
        public AuditableUserDto LastModifierUser { get; init; }

        //base profile
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string EmailAddress { get; init; } = string.Empty;
        public string FullName
        {
            get { return $"{FirstName} {LastName}"; }
        }
        public bool IsAdmin { get; init; }

        public IEnumerable<UserGroupDto> UserGroups { get; init; } = new List<UserGroupDto>();

        public dynamic CustomAttributes { get; init; }

        public static Expression<Func<User, UserDto>> GetProjection()
        {
            return entity => GetProjection(entity);
        }

        public static UserDto GetProjection(User entity)
        {
            if (entity == null)
                return null;

            return new UserDto
            {
                Id = entity.Id,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                EmailAddress = entity.EmailAddress,
                LastLoggedInTime = entity.LastLoggedInTime,
                IsActive = entity.IsActive,
                CreatorUserId = entity.CreatorUserId,
                CreationTime = entity.CreationTime,
                LastModificationTime = entity.LastModificationTime,
                LastModifierUserId = entity.LastModifierUserId,
                IsAdmin = entity.IsAdmin,
                CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
                LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
                UserGroups = entity.UserGroups != null ? entity.UserGroups.AsQueryable().Select(UserGroupDto.GetProjection()) : new List<UserGroupDto>(),
            };
        }
    }
}
