using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Raytha.Application.Common.Security;
using Raytha.Application.Common.Utils;
using Raytha.Application.Login.Queries;

namespace Raytha.Web.Authentication;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly IMediator _mediator;

    public CustomCookieAuthenticationEvents(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// The admin SPA and any other JSON client under <c>/raytha/api</c> need status codes, not
    /// redirects to the login page.
    /// </summary>
    private static bool IsApiRequest(HttpRequest request) =>
        request.Path.StartsWithSegments("/raytha/api");

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (!IsApiRequest(context.Request))
        {
            return base.RedirectToLogin(context);
        }

        return WriteProblem(
            context.Response,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "You must sign in to access this resource."
        );
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (!IsApiRequest(context.Request))
        {
            return base.RedirectToAccessDenied(context);
        }

        return WriteProblem(
            context.Response,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "You do not have permission to access this resource."
        );
    }

    private static Task WriteProblem(HttpResponse response, int status, string title, string detail)
    {
        response.StatusCode = status;
        response.ContentType = "application/problem+json";
        response.Headers.CacheControl = "no-store";
        return response.WriteAsync(
            JsonSerializer.Serialize(
                new
                {
                    type = $"https://httpstatuses.io/{status}",
                    title,
                    status,
                    detail,
                }
            )
        );
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userPrincipal = context.Principal;

        // Look for the LastChanged claim.
        var lastModifiedAsString = userPrincipal
            .Claims.FirstOrDefault(p => p.Type == "LastModificationTime")
            ?.Value;
        var userIdAsString = userPrincipal
            .Claims.FirstOrDefault(p => p.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        if (lastModifiedAsString == null || userIdAsString == null)
            return;

        var user = await _mediator.Send(
            new GetUserForAuthenticationById.Query { Id = userIdAsString }
        );

        if (user == null || !user.Success || !user.Result.IsActive)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );
            return;
        }

        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Result.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Result.EmailAddress),
            new Claim(ClaimTypes.GivenName, user.Result.FirstName),
            new Claim(ClaimTypes.Surname, user.Result.LastName),
            new Claim(
                RaythaClaimTypes.LastModificationTime,
                user.Result.LastModificationTime.ToString()
            ),
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

        ClaimsIdentity identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        context.ReplacePrincipal(principal);
        context.ShouldRenew = true;
    }
}
