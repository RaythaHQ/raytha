using Microsoft.AspNetCore.Mvc;
using Raytha.Application.AuthenticationSchemes.Queries;
using Raytha.Domain.ValueObjects;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class LoginWithSso : BaseAdminLoginPageModel
{
    public async Task<IActionResult> OnGet(string developerName, string returnUrl = "")
    {
        try
        {
            var response = await Mediator.Send(
                new GetAuthenticationSchemeByName.Query { DeveloperName = developerName }
            );

            if (!response.Result.IsEnabledForAdmins)
            {
                return Unauthorized();
            }

            if (
                response.Result.AuthenticationSchemeType.DeveloperName
                == AuthenticationSchemeType.Jwt.DeveloperName
            )
            {
                return Redirect(
                    RelativeUrlBuilder.GetSingleSignOnCallbackJwtUrl(
                        "Admin",
                        response.Result.DeveloperName,
                        response.Result.SignInUrl,
                        returnUrl
                    )
                );
            }

            if (
                response.Result.AuthenticationSchemeType.DeveloperName
                == AuthenticationSchemeType.Saml.DeveloperName
            )
            {
                return Redirect(
                    RelativeUrlBuilder.GetSingleSignOnCallbackSamlUrl(
                        "Admin",
                        response.Result.DeveloperName,
                        response.Result.SamlIdpEntityId,
                        response.Result.SignInUrl,
                        returnUrl
                    )
                );
            }

            throw new NotImplementedException();
        }
        catch (Exception e)
        {
            return RedirectToPage("/Login/LoginWithEmailAndPassword", new { area = "Admin" });
        }
    }
}
