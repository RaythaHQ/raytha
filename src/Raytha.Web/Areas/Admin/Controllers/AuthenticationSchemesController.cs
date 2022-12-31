using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raytha.Application.AuthenticationSchemes.Commands;
using Raytha.Application.AuthenticationSchemes.Queries;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.AuthenticationSchemes;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Filters;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class AuthenticationSchemesController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/settings/authentication-schemes", Name = "authenticationschemesindex")]
    public async Task<IActionResult> Index(string search = "", string orderBy = $"Label {SortOrder.ASCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetAuthenticationSchemes.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new AuthenticationSchemesListItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            LastModificationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.LastModificationTime),
            LastModifierUser = p.LastModifierUser != null ? p.LastModifierUser.FullName : "N/A",
            AuthenticationSchemeType = p.AuthenticationSchemeType.Label,
            IsEnabledForAdmins = p.IsEnabledForAdmins.YesOrNo(),
            IsEnabledForUsers = p.IsEnabledForUsers.YesOrNo()
        });

        var viewModel = new List_ViewModel<AuthenticationSchemesListItem_ViewModel>(items, response.Result.TotalCount);
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/authentication-schemes/create", Name = "authenticationschemescreate")]
    public IActionResult Create()
    {
        var supportedAuthenticationTypes = new OrderedDictionary()
        {
            { "", "-- SELECT --" },
            { AuthenticationSchemeType.Jwt.DeveloperName, AuthenticationSchemeType.Jwt.Label },
            { AuthenticationSchemeType.Saml.DeveloperName, AuthenticationSchemeType.Saml.Label }
        };
        var viewModel = new AuthenticationSchemesCreate_ViewModel
        {
            SupportedAuthenticationSchemeTypes = new SelectList(supportedAuthenticationTypes, "Key", "Value")
        };
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/authentication-schemes/create", Name = "authenticationschemescreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AuthenticationSchemesCreate_ViewModel model)
    {
        var input = new CreateAuthenticationScheme.Command
        {
            Label = model.Label,
            AuthenticationSchemeType = model.AuthenticationSchemeType,
            SignInUrl = model.SignInUrl,
            SignOutUrl = model.SignOutUrl,
            SamlCertificate = model.SamlCertificate,
            JwtSecretKey = model.JwtSecretKey,
            LoginButtonText = model.LoginButtonText,
            IsEnabledForAdmins = model.IsEnabledForAdmins,
            IsEnabledForUsers = model.IsEnabledForUsers,
            DeveloperName = model.DeveloperName,
            SamlIdpEntityId = model.SamlIdpEntityId,
            JwtUseHighSecurity = model.JwtUseHighSecurity
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Authentication scheme was created successfully.");
            return RedirectToAction("Edit", new { id = response.Result });
        }
        else
        {
            var supportedAuthenticationTypes = new OrderedDictionary()
            {
                { "", "-- SELECT --" },
                { AuthenticationSchemeType.Jwt.DeveloperName, AuthenticationSchemeType.Jwt.Label },
                { AuthenticationSchemeType.Saml.DeveloperName, AuthenticationSchemeType.Saml.Label }
            };
            model.SupportedAuthenticationSchemeTypes = new SelectList(supportedAuthenticationTypes, "Key", "Value");
            SetErrorMessage("There was an error attempting to create this authentication scheme. See the error below.", response.GetErrors());
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/authentication-schemes/edit/{id}", Name = "authenticationschemesedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var supportedAuthenticationTypes = new OrderedDictionary()
        {
            { AuthenticationSchemeType.EmailAndPassword.DeveloperName, AuthenticationSchemeType.EmailAndPassword.Label },
            { AuthenticationSchemeType.MagicLink.DeveloperName, AuthenticationSchemeType.MagicLink.Label },
            { AuthenticationSchemeType.Jwt.DeveloperName, AuthenticationSchemeType.Jwt.Label },
            { AuthenticationSchemeType.Saml.DeveloperName, AuthenticationSchemeType.Saml.Label }
        };
        var response = await Mediator.Send(new GetAuthenticationSchemeById.Query { Id = id });

        var model = new AuthenticationSchemesEdit_ViewModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
            IsBuiltInAuth = response.Result.IsBuiltInAuth,
            IsEnabledForAdmins = response.Result.IsEnabledForAdmins,
            IsEnabledForUsers = response.Result.IsEnabledForUsers,
            JwtSecretKey = response.Result.JwtSecretKey,
            SamlCertificate = response.Result.SamlCertificate,
            AuthenticationSchemeType = response.Result.AuthenticationSchemeType,
            SignInUrl = response.Result.SignInUrl,
            SignOutUrl = response.Result.SignOutUrl,
            LoginButtonText = response.Result.LoginButtonText,
            SamlIdpEntityId = response.Result.SamlIdpEntityId,
            JwtUseHighSecurity = response.Result.JwtUseHighSecurity,
            MagicLinkExpiresInSeconds = response.Result.MagicLinkExpiresInSeconds,
            SupportedAuthenticationSchemeTypes = new SelectList(supportedAuthenticationTypes, "Key", "Value")
        };
        
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/authentication-schemes/edit/{id}", Name = "authenticationschemesedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AuthenticationSchemesEdit_ViewModel model, string id)
    {
        var input = new EditAuthenticationScheme.Command
        {
            Id = id,
            Label = model.Label,
            AuthenticationSchemeType = model.AuthenticationSchemeType,
            SignInUrl = model.SignInUrl,
            SignOutUrl = model.SignOutUrl,
            SamlCertificate = model.SamlCertificate,
            JwtSecretKey = model.JwtSecretKey,
            LoginButtonText = model.LoginButtonText,
            IsEnabledForAdmins = model.IsEnabledForAdmins,
            IsEnabledForUsers = model.IsEnabledForUsers,
            SamlIdpEntityId = model.SamlIdpEntityId,
            JwtUseHighSecurity = model.JwtUseHighSecurity,
            MagicLinkExpiresInSeconds = model.MagicLinkExpiresInSeconds
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Authentication scheme was updated successfully.");
            return RedirectToAction("Edit", new { id });
        }
        else
        {
            var supportedAuthenticationTypes = new OrderedDictionary()
            {
                { AuthenticationSchemeType.EmailAndPassword.DeveloperName, AuthenticationSchemeType.EmailAndPassword.Label },
                { AuthenticationSchemeType.MagicLink.DeveloperName, AuthenticationSchemeType.MagicLink.Label },
                { AuthenticationSchemeType.Jwt.DeveloperName, AuthenticationSchemeType.Jwt.Label },
                { AuthenticationSchemeType.Saml.DeveloperName, AuthenticationSchemeType.Saml.Label }
            };
            SetErrorMessage("There was an error attempting to update this authentication scheme. See the error below.", response.GetErrors());
            model.Id = id;
            model.SupportedAuthenticationSchemeTypes = new SelectList(supportedAuthenticationTypes, "Key", "Value");
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/authentication-schemes/delete/{id}", Name = "authenticationschemesdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var input = new DeleteAuthenticationScheme.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Authentication scheme has been deleted.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage("There was an error deleting this authentication scheme", response.GetErrors());
            return RedirectToAction("Edit", new { id });
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Authentication Schemes";
        ViewData["ExpandSettingsMenu"] = true;
    }
}
