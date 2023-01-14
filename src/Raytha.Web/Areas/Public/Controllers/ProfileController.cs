using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.Login.Commands;
using Raytha.Application.Users;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Public.DbViewEngine;
using Raytha.Web.Areas.Public.Views.Profile;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Public.Controllers;

[Area("Public")]
[Authorize]
public class ProfileController : BaseController
{
    [Route("account/me", Name = "userprofile")]
    public async Task<IActionResult> Profile()
    {
        ChangeProfileSubmit_RenderModel viewModel = new ChangeProfileSubmit_RenderModel
        {
            RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
        };

        return new AccountActionViewResult(BuiltInWebTemplate.ChangeProfilePage, viewModel, ViewData);
    }

    [Route("account/me", Name = "userprofile")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ChangeProfile_ViewModel model)
    {
        var response = await Mediator.Send(new ChangeProfile.Command
        {
            Id = CurrentUser.UserId.Value,
            FirstName = model.FirstName,
            LastName = model.LastName
        });

        if (response.Success)
        {
            ChangeProfileSubmit_RenderModel viewModel = new ChangeProfileSubmit_RenderModel
            {
                SuccessMessage = "Profile successfully updated.",
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            };

            var identity = HttpContext.User.Identity as ClaimsIdentity;

            // check for existing claim and remove it
            var givenNameClaim = identity.FindFirst(ClaimTypes.GivenName);
            if (givenNameClaim != null)
                identity.RemoveClaim(givenNameClaim);

            var surnameClaim = identity.FindFirst(ClaimTypes.Surname);
            if (surnameClaim != null)
                identity.RemoveClaim(surnameClaim);

            identity.AddClaim(new Claim(ClaimTypes.GivenName, model.FirstName));
            identity.AddClaim(new Claim(ClaimTypes.Surname, model.LastName));

            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(principal), new AuthenticationProperties() { IsPersistent = true });

            return new AccountActionViewResult(BuiltInWebTemplate.ChangeProfilePage, viewModel, ViewData);
        }
        else
        {
            ChangeProfileSubmit_RenderModel viewModel = new ChangeProfileSubmit_RenderModel
            {
                ValidationFailures = response.GetErrors()?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage),
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            };

            return new AccountActionViewResult(BuiltInWebTemplate.ChangeProfilePage, viewModel, ViewData);
        }
    }

    [Route("account/me/change-password", Name = "userchangepassword")]
    public async Task<IActionResult> ChangePassword()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel(), ViewData);
        }

        ChangePasswordSubmit_RenderModel viewModel = new ChangePasswordSubmit_RenderModel
        {
            RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
        };

        return new AccountActionViewResult(BuiltInWebTemplate.ChangePasswordPage, viewModel, ViewData);
    }

    [Route("account/me/change-password", Name = "userchangepassword")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePassword_ViewModel model)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel(), ViewData);
        }

        var response = await Mediator.Send(new ChangePassword.Command
        {
            Id = CurrentUser.UserId.Value,
            CurrentPassword = model.CurrentPassword,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword
        });

        if (response.Success)
        {
            ChangePasswordSubmit_RenderModel viewModel = new ChangePasswordSubmit_RenderModel
            {
                SuccessMessage = "Password successfully updated.",
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            };

            return new AccountActionViewResult(BuiltInWebTemplate.ChangePasswordPage, viewModel, ViewData);
        }
        else
        {
            ChangePasswordSubmit_RenderModel viewModel = new ChangePasswordSubmit_RenderModel
            {
                ValidationFailures = response.GetErrors()?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage),
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            };

            return new AccountActionViewResult(BuiltInWebTemplate.ChangePasswordPage, viewModel, ViewData);
        }
    }
}

