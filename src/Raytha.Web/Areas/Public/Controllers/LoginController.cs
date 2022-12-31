using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.AuthenticationSchemes.Queries;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Application.Common.Utils;
using Raytha.Application.Login;
using Raytha.Application.Login.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Public.DbViewEngine;
using Raytha.Web.Areas.Public.Views.Login;
using Microsoft.AspNetCore.Http;
using Raytha.Application.Common.Models.RenderModels;
using Azure;
using Raytha.Application.Login.Queries;

namespace Raytha.Web.Areas.Public.Controllers;

[Area("Public")]
public class LoginController : BaseController
{
    [Route("account/login", Name = "userloginemailandpassword")]
    public async Task<IActionResult> LoginWithEmailAndPassword(string returnUrl = null)
    {
        var response = await Mediator.Send(new GetAuthenticationSchemes.Query
        {
            IsEnabledForUsers = true,
            PageSize = int.MaxValue
        });

        if (OnlyHasSingleSignOnEnabled(response.Result))
        {
            var singleSignOnScheme = response.Result.Items.First();
            return Redirect(GetSingleSignOnCallbackUrl(singleSignOnScheme, returnUrl));
        }

        if (BuiltInAuthIsMagicLinkOnly(response.Result))
            return RedirectToAction("LoginWithMagicLink", "Login", new { returnUrl });

        LoginSubmit_RenderModel viewModel = new LoginSubmit_RenderModel
        {
            ReturnUrl = returnUrl,
            RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken,
            AuthenticationSchemes = CurrentOrganization_RenderModel.GetProjection(CurrentOrganization).AuthenticationSchemes.Where(p => p.IsEnabledForUsers)
        };

        return new AccountActionViewResult(BuiltInWebTemplate.LoginWithEmailAndPasswordPage, viewModel);
    }

    [Route("account/login", Name = "userloginemailandpassword")]
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
                return RedirectToAction("Index", "Main");
            }
        }
        else
        {
            LoginSubmit_RenderModel viewModel = new LoginSubmit_RenderModel
            {
                ReturnUrl = returnUrl,
                ValidationFailures = response.GetErrors()?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage),
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken,
                AuthenticationSchemes = CurrentOrganization_RenderModel.GetProjection(CurrentOrganization).AuthenticationSchemes.Where(p => p.IsEnabledForUsers)
            };

            return new AccountActionViewResult(BuiltInWebTemplate.LoginWithEmailAndPasswordPage, viewModel);
        }
    }

    [Route("account/login/magic-link", Name = "userloginmagiclink")]
    public async Task<IActionResult> LoginWithMagicLink(string returnUrl = null)
    {
        var response = await Mediator.Send(new GetAuthenticationSchemes.Query
        {
            IsEnabledForUsers = true,
            PageSize = int.MaxValue
        });

        if (OnlyHasSingleSignOnEnabled(response.Result))
        {
            var authScheme = response.Result.Items.First();
            return Redirect(GetSingleSignOnCallbackUrl(authScheme, returnUrl));
        }

        if (BuiltInAuthIsEmailAndPasswordOnly(response.Result))
            return RedirectToAction("LoginWithEmailAndPassword", "Login", new { returnUrl });

        LoginSubmit_RenderModel viewModel = new LoginSubmit_RenderModel
        {
            ReturnUrl = returnUrl,
            ValidationFailures = response.GetErrors()?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage),
            RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken,
            AuthenticationSchemes = CurrentOrganization_RenderModel.GetProjection(CurrentOrganization).AuthenticationSchemes.Where(p => p.IsEnabledForUsers)
        };

        return new AccountActionViewResult(BuiltInWebTemplate.LoginWithMagicLinkPage, viewModel);
    }

    [Route("account/login/magic-link", Name = "userloginmagiclink")]
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
            LoginSubmit_RenderModel viewModel = new LoginSubmit_RenderModel
            {
                ReturnUrl = returnUrl,
                ValidationFailures = response.GetErrors()?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage),
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken,
                AuthenticationSchemes = CurrentOrganization_RenderModel.GetProjection(CurrentOrganization).AuthenticationSchemes.Where(p => p.IsEnabledForUsers)
            };

            return new AccountActionViewResult(BuiltInWebTemplate.LoginWithMagicLinkPage, viewModel);
        }
    }

    [Route("account/login/magic-link/sent", Name = "userloginmagiclinksent")]
    public IActionResult LoginWithMagicLinkSent()
    {
        var viewModel = new EmptyTarget_RenderModel();
        return new AccountActionViewResult(BuiltInWebTemplate.LoginWithMagicLinkSentPage, viewModel);
    }

    [Route("account/login/magic-link/complete/{token?}", Name = "userloginmagiclinkcomplete")]
    public async Task<IActionResult> LoginWithMagicLinkComplete(string token = null, string returnUrl = null)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
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
                return RedirectToAction("Index", "Main");
            }
        }
        else
        {
            return RedirectToAction("LoginWithMagicLink", "Login", new { returnUrl });
        }
    }

    [Route("account/login/sso/{developerName}", Name = "userloginsso")]
    public async Task<IActionResult> BeginSingleSignOnHandshake(string developerName, string returnUrl = null)
    {
        try
        {
            var response = await Mediator.Send(new GetAuthenticationSchemeByName.Query { DeveloperName = developerName });
            return Redirect(GetSingleSignOnCallbackUrl(response.Result, returnUrl));
        }
        catch (Exception e)
        {
            return RedirectToAction("Index", "Main");
        }
    }

    [Route("account/login/jwt/{developerName}", Name = "userloginjwt")]
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

    [Route("account/login/saml/{developerName}", Name = "userloginsaml")]
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

    [Route("account/logout", Name = "userlogout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("LoginWithEmailAndPassword", "Login", new { area = "Public" });
    }

    [Route("account/login/forgot-password/begin", Name = "userforgotpasswordbegin")]
    public async Task<IActionResult> ForgotPassword()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
        }

        ForgotPasswordSubmit_RenderModel viewModel = new ForgotPasswordSubmit_RenderModel
        {
            RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
        };

        return new AccountActionViewResult(BuiltInWebTemplate.ForgotPasswordPage, viewModel);
        
    }

    [Route("account/login/forgot-password/begin", Name = "userforgotpasswordbegin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(BeginForgotPassword_ViewModel model)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
        }

        var response = await Mediator.Send(new BeginForgotPassword.Command { EmailAddress = model.EmailAddress });
        if (response.Success)
        {
            return new AccountActionViewResult(BuiltInWebTemplate.ForgotPasswordResetLinkSentPage, new EmptyTarget_RenderModel());
        }
        else
        {
            ForgotPasswordSubmit_RenderModel viewModel = new ForgotPasswordSubmit_RenderModel
            {
                ValidationFailures = response.GetErrors()?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage),
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            };

            return new AccountActionViewResult(BuiltInWebTemplate.ForgotPasswordPage, viewModel);
        }
    }

    [Route("account/login/forgot-password/sent", Name = "userforgotpasswordsent")]
    public IActionResult ForgotPasswordSent()
    {
        return new AccountActionViewResult(BuiltInWebTemplate.ForgotPasswordResetLinkSentPage, new EmptyTarget_RenderModel());
    }

    [Route("account/login/forgot-password/complete/{token?}", Name = "userforgotpasswordcomplete")]
    public async Task<IActionResult> ForgotPasswordComplete(string token)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
        }

        if (string.IsNullOrEmpty(token))
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
        }

        var isTokenValidResult = await Mediator.Send(new GetForgotPasswordTokenValidity.Query { Id = token });
        if (!isTokenValidResult.Success)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
        }

        ForgotPasswordCompleteSubmit_RenderModel viewModel = new ForgotPasswordCompleteSubmit_RenderModel
        {
            Token = token,
            RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
        };

        return new AccountActionViewResult(BuiltInWebTemplate.ForgotPasswordCompletePage, viewModel);
    }

    [Route("account/login/forgot-password/complete/{token?}", Name = "userforgotpasswordcomplete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPasswordComplete(CompleteForgotPassword_ViewModel model, string token)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
        }

        if (string.IsNullOrEmpty(token))
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
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
            return new AccountActionViewResult(BuiltInWebTemplate.ForgotPasswordSuccessPage, new EmptyTarget_RenderModel());
        }
        else
        {
            ForgotPasswordCompleteSubmit_RenderModel viewModel = new ForgotPasswordCompleteSubmit_RenderModel
            {
                Token = token,
                ValidationFailures = response.GetErrors()?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage),
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            };
            return new AccountActionViewResult(BuiltInWebTemplate.ForgotPasswordCompletePage, viewModel);
        }
    }

    [Route("account/create", Name = "usercreate")]
    public async Task<IActionResult> CreateUser()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
        }

        CreateUserSubmit_RenderModel viewModel = new CreateUserSubmit_RenderModel
        {
            RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
        };

        return new AccountActionViewResult(BuiltInWebTemplate.UserRegistrationForm, viewModel);
    }

    [Route("account/create", Name = "usercreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUser_ViewModel model)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel());
        }

        var command = new CreateUser.Command
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailAddress = model.EmailAddress,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword
        };

        var response = await Mediator.Send(command);
        if (response.Success)
        {
            return new AccountActionViewResult(BuiltInWebTemplate.UserRegistrationFormSuccess, new EmptyTarget_RenderModel());
        }
        else
        {
            CreateUserSubmit_RenderModel viewModel = new CreateUserSubmit_RenderModel
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailAddress = model.EmailAddress,
                ValidationFailures = response.GetErrors()?.ToDictionary(k => k.PropertyName, v => v.ErrorMessage),
                RequestVerificationToken = Antiforgery.GetAndStoreTokens(HttpContext).RequestToken
            };
            return new AccountActionViewResult(BuiltInWebTemplate.UserRegistrationForm, viewModel);
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
                return RedirectToAction("Index", "Main");
            }
        }
        else
        {
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, new GenericError_RenderModel
            {
                ErrorMessage = result.Error
            });
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
        if (!authScheme.IsEnabledForUsers)
            throw new Exception("Authentication scheme is disabled");

        if (authScheme.AuthenticationSchemeType == AuthenticationSchemeType.Jwt.DeveloperName)
        {
            string callbackUrl = Url.ActionLink("Jwt", "Login", values: new { developerName = authScheme.DeveloperName });
            if (!string.IsNullOrEmpty(returnUrl))
            {
                var parametersToAdd = new Dictionary<string, string> { { "returnUrl", returnUrl } };
                callbackUrl = QueryHelpers.AddQueryString(callbackUrl, parametersToAdd);
            }
            var setCallbackParams = new Dictionary<string, string> { { "raytha_callback_url", callbackUrl } };
            var loginUrl = QueryHelpers.AddQueryString(authScheme.SignInUrl, setCallbackParams);
            return loginUrl;
        }
        else if (authScheme.AuthenticationSchemeType == AuthenticationSchemeType.Saml.DeveloperName)
        {
            var acsUrl = Url.ActionLink("Saml", "Login", values: new { developerName = authScheme.DeveloperName });
            var samlRequest = SamlUtility.GetSamlRequestAsBase64(acsUrl, authScheme.SamlIdpEntityId);
            var parametersToAdd = new Dictionary<string, string> { { "SAMLRequest", samlRequest } };

            if (!string.IsNullOrEmpty(returnUrl))
                parametersToAdd.Add("RelayState", returnUrl);

            var loginUrl = QueryHelpers.AddQueryString(authScheme.SignInUrl, parametersToAdd);
            return loginUrl;
        }
        else
            throw new Exception("Unknown Sso type");
    }
}


