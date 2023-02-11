using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.AuthenticationSchemes.Queries;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.Common.Security;
using Raytha.Application.Common.Utils;
using Raytha.Application.Login;
using Raytha.Application.Login.Commands;
using Raytha.Application.Login.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.Login;
using Raytha.Web.Areas.Public.DbViewEngine;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class LoginController : BaseController
{
    [Route(RAYTHA_ROUTE_PREFIX + "/login", Name = "adminloginemailandpassword")]
    public async Task<IActionResult> LoginWithEmailAndPassword(string returnUrl = null)
    {
        var response = await Mediator.Send(new GetAuthenticationSchemes.Query
        {
            IsEnabledForAdmins = true,
            PageSize = int.MaxValue
        });

        if (OnlyHasSingleSignOnEnabled(response.Result))
        {
            var singleSignOnScheme = response.Result.Items.First();
            return Redirect(GetSingleSignOnCallbackUrl(singleSignOnScheme, returnUrl));
        }

        ViewData["returnUrl"] = returnUrl;
        if (BuiltInAuthIsMagicLinkOnly(response.Result))
            return RedirectToAction("LoginWithMagicLink", "Login", new { returnUrl });

        var items = response.Result.Items.Select(p => new LoginAuthenticationSchemeChoiceItem_ViewModel
        {
            DeveloperName = p.DeveloperName,
            AuthenticationSchemeType = p.AuthenticationSchemeType,
            LoginButtonText = p.LoginButtonText
        });

        var viewModel = new LoginWithEmailAndPassword_ViewModel { AuthenticationSchemes = items };
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login", Name = "adminloginemailandpassword")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithEmailAndPassword(LoginWithEmailAndPassword_ViewModel model, string returnUrl = "")
    {
        var input = new LoginWithEmailAndPassword.Command 
        { 
            EmailAddress = model.EmailAddress,
            Password = model.Password
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            await LoginWithClaims(response.Result, model.RememberMe);
            if (HasLocalRedirect(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
        else
        {
            var authSchemes = await Mediator.Send(new GetAuthenticationSchemes.Query
            {
                IsEnabledForAdmins = true,
                PageSize = int.MaxValue
            });

            var items = authSchemes.Result.Items.Select(p => new LoginAuthenticationSchemeChoiceItem_ViewModel
            {
                DeveloperName = p.DeveloperName,
                AuthenticationSchemeType = p.AuthenticationSchemeType,
                LoginButtonText = p.LoginButtonText
            });

            var viewModel = new LoginWithEmailAndPassword_ViewModel { AuthenticationSchemes = items };
            SetErrorMessage(response.Error);
            this.HttpContext.Response.StatusCode = 303;

            return View(viewModel);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/magic-link", Name = "adminloginmagiclink")]
    public async Task<IActionResult> LoginWithMagicLink(string returnUrl = null)
    {
        var response = await Mediator.Send(new GetAuthenticationSchemes.Query
        {
            IsEnabledForAdmins = true,
            PageSize = int.MaxValue
        });

        if (OnlyHasSingleSignOnEnabled(response.Result))
        {
            var authScheme = response.Result.Items.First();
            return Redirect(GetSingleSignOnCallbackUrl(authScheme, returnUrl));
        }

        ViewData["returnUrl"] = returnUrl;
        if (BuiltInAuthIsEmailAndPasswordOnly(response.Result))
            return RedirectToAction("LoginWithEmailAndPassword", "Login", new { returnUrl });

        var items = response.Result.Items.Select(p => new LoginAuthenticationSchemeChoiceItem_ViewModel
        {
            DeveloperName = p.DeveloperName,
            AuthenticationSchemeType = p.AuthenticationSchemeType,
            LoginButtonText = p.LoginButtonText
        });

        var viewModel = new LoginWithMagicLink_ViewModel { AuthenticationSchemes = items };
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/magic-link", Name = "adminloginmagiclink")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithMagicLink(LoginWithMagicLink_ViewModel model, string returnUrl = null)
    {
        var response = await Mediator.Send(new BeginLoginWithMagicLink.Command
        { 
            EmailAddress = model.EmailAddress,
            ReturnUrl = returnUrl
        });

        if (response.Success)
        {
            return RedirectToAction("LoginWithMagicLinkSent", "Login");
        }
        else
        {
            var authSchemes = await Mediator.Send(new GetAuthenticationSchemes.Query
            {
                IsEnabledForAdmins = true,
                PageSize = 1000
            });

            var items = authSchemes.Result.Items.Select(p => new LoginAuthenticationSchemeChoiceItem_ViewModel
            {
                DeveloperName = p.DeveloperName,
                AuthenticationSchemeType = p.AuthenticationSchemeType,
                LoginButtonText = p.LoginButtonText
            });

            var viewModel = new LoginWithMagicLink_ViewModel { AuthenticationSchemes = items };
            SetErrorMessage(response.Error);
            this.HttpContext.Response.StatusCode = 303;

            return View(viewModel);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/magic-link/sent", Name = "adminloginmagiclinksent")]
    public IActionResult LoginWithMagicLinkSent()
    {
        return View();
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/magic-link/complete/{token?}", Name = "adminloginmagiclinkcomplete")]
    public async Task<IActionResult> LoginWithMagicLinkComplete(string token = null, string returnUrl = null)
    {
        if (string.IsNullOrEmpty(token))
        {
            SetErrorMessage("Login token is missing.");
            return new ForbidResult();
        }
     
        var response = await Mediator.Send(new CompleteLoginWithMagicLink.Command { Id = token });

        if (response.Success)
        {
            await LoginWithClaims(response.Result, true);
            if (HasLocalRedirect(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
        else
        {
            SetErrorMessage(response.Error);
            return RedirectToAction("LoginWithMagicLink", "Login", new { returnUrl });
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/sso/{developerName}", Name = "adminloginsso")]
    public async Task<IActionResult> BeginSingleSignOnHandshake(string developerName, string returnUrl = null)
    {
        try
        {
            var response = await Mediator.Send(new GetAuthenticationSchemeByName.Query { DeveloperName = developerName });
            return Redirect(GetSingleSignOnCallbackUrl(response.Result, returnUrl));
        }
        catch (Exception e)
        {
            SetErrorMessage(e.Message);
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/jwt/{developerName}", Name = "adminloginjwt")]
    public async Task<IActionResult> Jwt(string developerName, string token, string returnUrl = null)
    {
        var command = new LoginWithJwt.Command 
        { 
            DeveloperName = developerName,
            Token = token
        };
        var response = await Mediator.Send(command);
        return await HandleSingleSignOnResult(response, returnUrl);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/saml/{developerName}", Name = "adminloginsaml")]
    [HttpPost]
    public async Task<IActionResult> Saml(string developerName)
    {
        var samlResponse = Request.Form["SAMLResponse"].ToString().Replace(" ", "+"); //can't figure out why spaces come in on the post
        string returnUrl = string.Empty;
        if (Request.Form.ContainsKey("RelayState") && Request.Form["RelayState"].Count == 1)
            returnUrl = Request.Form["RelayState"].ToArray()[0].ToString();

        var command = new LoginWithSaml.Command 
        { 
            DeveloperName = developerName,
            SAMLResponse = samlResponse
        };

        var response = await Mediator.Send(command);
        return await HandleSingleSignOnResult(response, returnUrl);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/forgot-password/begin", Name = "adminforgotpasswordbegin")]
    public async Task<IActionResult> ForgotPassword()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme disabled for administrators");
            return new ForbidResult();
        }
        else
        {
            var model = new BeginForgotPassword_ViewModel();
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/forgot-password/begin", Name = "adminforgotpasswordbegin")]
    [HttpPost]
    public async Task<IActionResult> ForgotPassword(BeginForgotPassword_ViewModel model)
    {
        var response = await Mediator.Send(new BeginForgotPassword.Command { EmailAddress = model.EmailAddress });
        if (response.Success)
        {
            return RedirectToAction("ForgotPasswordSent");
        }
        else
        {
            SetErrorMessage(response.Error);
            this.HttpContext.Response.StatusCode = 303;
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/forgot-password/sent", Name = "adminforgotpasswordsent")]
    public IActionResult ForgotPasswordSent()
    {
        return View();
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/forgot-password/complete/{token?}", Name = "adminforgotpasswordcomplete")]
    public async Task<IActionResult> ForgotPasswordComplete(string token)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme disabled for administrators");
            return new ForbidResult();
        }

        if (string.IsNullOrEmpty(token))
        {
            SetErrorMessage("Forgot password token is missing.");
            return new ForbidResult();
        }

        var isTokenValidResult = await Mediator.Send(new GetForgotPasswordTokenValidity.Query { Id = token });
        if (!isTokenValidResult.Success)
        {
            SetErrorMessage(isTokenValidResult.Error);
            return new ForbidResult();
        }

        var model = new CompleteForgotPassword_ViewModel { Id = token };
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login/forgot-password/complete/{token?}", Name = "adminforgotpasswordcomplete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPasswordComplete(CompleteForgotPassword_ViewModel model, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            SetErrorMessage("Forgot password token is missing.");
            return new ForbidResult();
        }

        var command = new CompleteForgotPassword.Command 
        { 
            Id = token,
            NewPassword = model.NewPassword,
            ConfirmNewPassword = model.ConfirmNewPassword,
        };

        var response = await Mediator.Send(command);
        if (response.Success)
        {
            SetSuccessMessage("Password changed successfully. Please login.");
            return RedirectToAction("LoginWithEmailAndPassword");
        }

        SetErrorMessage(response.Error);
        this.HttpContext.Response.StatusCode = 303;
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/logout", Name = "adminlogout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("LoginWithEmailAndPassword", "Login", new { area = "Admin"});
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/login-redirect", Name = "loginredirect")]
    public IActionResult LoginRedirect(string returnUrl = "")
    {
        if (returnUrl.StartsWith($"/{RAYTHA_ROUTE_PREFIX}"))
        {
            return RedirectToAction("LoginWithEmailAndPassword", "Login", new { area = "Admin", returnUrl });
        }
        else
        {
            return Redirect($"/account/login?returnUrl={returnUrl}");
        }
    }

    private async Task<IActionResult> HandleSingleSignOnResult(ICommandResponseDto<LoginDto> result, string returnUrl)
    {
        if (result.Success)
        {
            await LoginWithClaims(result.Result);
            if (HasLocalRedirect(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
        else 
        {
            SetErrorMessage(result.Error);
            return RedirectToAction("Index", "Dashboard");
        }
    }

    private async Task LoginWithClaims(LoginDto result, bool rememberMe = true)
    {
        List<Claim> claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Id.ToString()));
        claims.Add(new Claim(RaythaClaimTypes.LastModificationTime, result.LastModificationTime.ToString()));
        ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(principal), new AuthenticationProperties() { IsPersistent = rememberMe });
    }

    private bool HasLocalRedirect(string returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl);
    }

    private bool OnlyHasSingleSignOnEnabled(ListResultDto<AuthenticationSchemeDto> result)
    {
        return result.TotalCount == 1 && !result.Items.First().IsBuiltInAuth;
    }

    private bool BuiltInAuthIsMagicLinkOnly(ListResultDto<AuthenticationSchemeDto> result)
    {
        return !result.Items.Any(p => p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword.DeveloperName) && result.Items.Any(p => p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink.DeveloperName);
    }

    private bool BuiltInAuthIsEmailAndPasswordOnly(ListResultDto<AuthenticationSchemeDto> result)
    {
        return result.Items.Any(p => p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword.DeveloperName) && !result.Items.Any(p => p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink.DeveloperName);
    }

    private string GetSingleSignOnCallbackUrl(AuthenticationSchemeDto authScheme, string returnUrl)
    {
        if (!authScheme.IsEnabledForAdmins)
            throw new Exception("Authentication scheme is disabled");

        if (authScheme.AuthenticationSchemeType == AuthenticationSchemeType.Jwt.DeveloperName)
        {
            string callbackUrl = Url.ActionLink("Jwt", "Login", values: new { developerName = authScheme.DeveloperName });
            if (!string.IsNullOrEmpty(returnUrl))
            {
                var parametersToAdd = new Dictionary<string, string> { { "returnUrl", returnUrl } };
                callbackUrl = QueryHelpers.AddQueryString(callbackUrl, parametersToAdd);
            }
            var setCallbackParams = new Dictionary<string, string> { { "raytha_callback_url", callbackUrl }};
            var loginUrl = QueryHelpers.AddQueryString(authScheme.SignInUrl, setCallbackParams);
            return loginUrl;
        }
        else if (authScheme.AuthenticationSchemeType == AuthenticationSchemeType.Saml.DeveloperName)
        {
            var acsUrl = Url.ActionLink("Saml", "Login", values: new { developerName = authScheme.DeveloperName });
            var samlRequest = SamlUtility.GetSamlRequestAsBase64(acsUrl, authScheme.SamlIdpEntityId);
            var parametersToAdd = new Dictionary<string, string> { { "SAMLRequest", samlRequest }};
            
            if (!string.IsNullOrEmpty(returnUrl))
                parametersToAdd.Add("RelayState", returnUrl);

            var loginUrl = QueryHelpers.AddQueryString(authScheme.SignInUrl, parametersToAdd);
            return loginUrl;
        }
        else
            throw new Exception("Unknown Sso type");
    }
}
