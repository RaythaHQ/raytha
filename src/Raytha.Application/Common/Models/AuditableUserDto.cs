using Raytha.Domain.Entities;
using System.Linq.Expressions;

namespace Raytha.Application.Common.Models;

public record AuditableUserDto : BaseEntityDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string FullName
    {
        get
        {
            if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
                return string.Empty;

            return $"{FirstName} {LastName}";
        }
    }
    public static Expression<Func<User?, AuditableUserDto?>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static AuditableUserDto? GetProjection(User? entity)
    {
        if (entity == null)
            return null;

        return new AuditableUserDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            EmailAddress = entity.EmailAddress
        };
    }
}
