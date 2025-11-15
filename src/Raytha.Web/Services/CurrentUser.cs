using System;
using System.Linq;
using System.Security.Claims;
using CSharpVitamins;
using Microsoft.AspNetCore.Http;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Security;

namespace Raytha.Web.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor != null
        && _httpContextAccessor.HttpContext != null
        && _httpContextAccessor.HttpContext.User != null
        && _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated;

    public ShortGuid? UserId =>
        IsAuthenticated
            ? _httpContextAccessor
                ?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                ?.Value
            : null;
    public string FirstName =>
        IsAuthenticated
            ? _httpContextAccessor
                ?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)
                ?.Value
            : string.Empty;
    public string LastName =>
        IsAuthenticated
            ? _httpContextAccessor
                ?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)
                ?.Value
            : string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string EmailAddress =>
        IsAuthenticated
            ? _httpContextAccessor
                ?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)
                ?.Value
            : string.Empty;
    public string SsoId =>
        _httpContextAccessor
            ?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == RaythaClaimTypes.SsoId)
            ?.Value;
    public string AuthenticationScheme =>
        _httpContextAccessor
            ?.HttpContext.User.Claims.FirstOrDefault(c =>
                c.Type == RaythaClaimTypes.AuthenticationScheme
            )
            ?.Value;
    public string RemoteIpAddress =>
        _httpContextAccessor?.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
    public bool IsAdmin =>
        _httpContextAccessor
            ?.HttpContext.User.Claims.FirstOrDefault(c => c.Type == RaythaClaimTypes.IsAdmin)
            ?.Value != null
            ? Convert.ToBoolean(
                _httpContextAccessor
                    ?.HttpContext.User.Claims.FirstOrDefault(c =>
                        c.Type == RaythaClaimTypes.IsAdmin
                    )
                    ?.Value
            )
            : false;

    public DateTime? LastModificationTime
    {
        get
        {
            if (IsAuthenticated)
            {
                var lastModified = _httpContextAccessor
                    ?.HttpContext.User.Claims.FirstOrDefault(c =>
                        c.Type == RaythaClaimTypes.LastModificationTime
                    )
                    ?.Value;
                if (!string.IsNullOrEmpty(lastModified))
                    return Convert.ToDateTime(lastModified);
            }
            return null;
        }
    }

    public string[] Roles =>
        _httpContextAccessor
            ?.HttpContext.User.Claims.Where(c => c.Type == ClaimTypes.Role)
            .Select(p => p.Value)
            .ToArray();
    public string[] UserGroups =>
        _httpContextAccessor
            ?.HttpContext.User.Claims.Where(c => c.Type == RaythaClaimTypes.UserGroups)
            .Select(p => p.Value)
            .ToArray();
    public string[] SystemPermissions =>
        _httpContextAccessor
            ?.HttpContext.User.Claims.Where(c => c.Type == RaythaClaimTypes.SystemPermissions)
            .Select(p => p.Value)
            .ToArray();
    public string[] ContentTypePermissions =>
        _httpContextAccessor
            ?.HttpContext.User.Claims.Where(c => c.Type == RaythaClaimTypes.ContentTypePermissions)
            .Select(p => p.Value)
            .ToArray();
}
