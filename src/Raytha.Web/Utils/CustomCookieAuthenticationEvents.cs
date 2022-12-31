using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Raytha.Application.Common.Security;
using Raytha.Application.Common.Utils;
using Raytha.Application.Login.Queries;

namespace Raytha.Web.Helpers;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly IMediator _mediator;

    public CustomCookieAuthenticationEvents(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userPrincipal = context.Principal;

        // Look for the LastChanged claim.
        var lastModifiedAsString = userPrincipal.Claims.FirstOrDefault(p => p.Type == "LastModificationTime")?.Value;
        var userIdAsString = userPrincipal.Claims.FirstOrDefault(p => p.Type == ClaimTypes.NameIdentifier)?.Value;

        if (lastModifiedAsString == null || userIdAsString == null)
            return;

        var user = await _mediator.Send(new GetUserForAuthenticationById.Query { Id = userIdAsString });

        if (user == null || !user.Success || !user.Result.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Result.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Result.EmailAddress),
            new Claim(ClaimTypes.GivenName, user.Result.FirstName),
            new Claim(ClaimTypes.Surname, user.Result.LastName),
            new Claim(RaythaClaimTypes.LastModificationTime, user.Result.LastModificationTime.ToString()),
            new Claim(RaythaClaimTypes.IsAdmin, user.Result.IsAdmin.ToString()),
            new Claim(RaythaClaimTypes.SsoId, user.Result.SsoId.IfNullOrEmpty(string.Empty)),
            new Claim(RaythaClaimTypes.AuthenticationScheme, user.Result.AuthenticationScheme),
        };

        var systemPermissions = new List<string>();
        var contentTypePermissions = new List<string>();

        foreach (var role in user.Result.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.DeveloperName.ToString()));
            systemPermissions.AddRange(role.SystemPermissions);

            foreach (var contentTypePermission in role.ContentTypePermissionsFriendlyNames)
            {
                foreach (var granularPermission in contentTypePermission.Value)
                {
                    contentTypePermissions.Add($"{contentTypePermission.Key}_{granularPermission}");
                }
            }    
        }

        foreach (var systemPermission in systemPermissions.Distinct())
        {
            claims.Add(new Claim(RaythaClaimTypes.SystemPermissions, systemPermission));
        }

        foreach (var contentTypePermission in contentTypePermissions.Distinct())
        {
            claims.Add(new Claim(RaythaClaimTypes.ContentTypePermissions, contentTypePermission));
        }

        foreach (var userGroup in user.Result.UserGroups)
        {
            claims.Add(new Claim(RaythaClaimTypes.UserGroups, userGroup.DeveloperName));
        }

        ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        context.ReplacePrincipal(principal);
        context.ShouldRenew = true;
    }
}