using CSharpVitamins;

namespace Raytha.Application.Common.Interfaces;

public interface ICurrentUser
{
    ShortGuid? UserId { get; }
    string FirstName { get; }
    string LastName { get; }
    string FullName { get; }
    string EmailAddress { get; }
    DateTime? LastModificationTime { get; }
    bool IsAuthenticated { get; }
    string SsoId { get; }
    string AuthenticationScheme { get; }
    string RemoteIpAddress { get; }
    bool IsAdmin { get; }
    public string[] Roles { get; }
    public string[] UserGroups { get; }
}
