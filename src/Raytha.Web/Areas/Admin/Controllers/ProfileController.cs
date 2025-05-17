using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Login.Commands;
using Raytha.Web.Areas.Admin.Views.Profile;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ProfileController : BaseController
{
    [Route(RAYTHA_ROUTE_PREFIX + "/profile", Name = "adminprofile")]
    public async Task<IActionResult> Profile()
    {
        var viewModel = new ChangeProfile_ViewModel
        {
            FirstName = CurrentUser.FirstName,
            LastName = CurrentUser.LastName,
            EmailAddress = CurrentUser.EmailAddress,
        };

        ViewData["ActiveMenu"] = "My Profile";
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/profile", Name = "adminprofile")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ChangeProfile_ViewModel model)
    {
        var response = await Mediator.Send(
            new ChangeProfile.Command
            {
                Id = CurrentUser.UserId.Value,
                FirstName = model.FirstName,
                LastName = model.LastName,
            }
        );

        if (response.Success)
        {
            SetSuccessMessage("Profile changed successfully.");
            return RedirectToAction("Profile");
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to change your profile. See the error below."
            );
            this.HttpContext.Response.StatusCode = 303;
            ViewData["ActiveMenu"] = "My Profile";
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/profile/change-password", Name = "adminchangepassword")]
    public async Task<IActionResult> ChangePassword()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToAction("Profile");
        }

        var model = new ChangePassword_ViewModel();
        ViewData["ActiveMenu"] = "Change Password";
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/profile/change-password", Name = "adminchangepassword")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePassword_ViewModel model)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToAction("Profile");
        }

        var response = await Mediator.Send(
            new ChangePassword.Command
            {
                Id = CurrentUser.UserId.Value,
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmNewPassword = model.ConfirmNewPassword,
            }
        );

        if (response.Success)
        {
            SetSuccessMessage("Password changed successfully.");
            return RedirectToAction("ChangePassword");
        }
        else
        {
            ViewData["ActiveMenu"] = "Change Password";
            SetErrorMessage(
                "There was an error attempting to change your password. See the error below."
            );
            this.HttpContext.Response.StatusCode = 303;
            return View(model);
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ExpandProfileMenu"] = true;
    }
}
