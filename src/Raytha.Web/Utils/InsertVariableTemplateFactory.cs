using Raytha.Application.Admins;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentTypes;
using Raytha.Application.Login;
using Raytha.Application.Users;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;
using System.Collections.Generic;
using System.Linq;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenus;

namespace Raytha.Web.Utils;

public class InsertVariableTemplateFactory
{
    static InsertVariableTemplateFactory()
    {
    }

    private InsertVariableTemplateFactory()
    {
    }

    private InsertVariableTemplateFactory(string developerName, string variableCategoryName, IInsertTemplateVariable templateInfo)
    {
        DeveloperName = developerName;
        TemplateInfo = templateInfo;
        VariableCategoryName = variableCategoryName;
    }

    public static InsertVariableTemplateFactory From(string developerName)
    {
        var type = Templates.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedTemplateTypeException(developerName);
        }

        return type;
    }

    public static InsertVariableTemplateFactory Request => new("Request", "Request", new Wrapper_RenderModel());
    public static InsertVariableTemplateFactory AdminWelcomeEmail => new(BuiltInEmailTemplate.AdminWelcomeEmail, "Target", new SendAdminWelcomeEmail_RenderModel());
    public static InsertVariableTemplateFactory AdminPasswordChangedEmail => new(BuiltInEmailTemplate.AdminPasswordChangedEmail, "Target", new SendAdminPasswordChanged_RenderModel());
    public static InsertVariableTemplateFactory AdminPasswordResetEmail => new(BuiltInEmailTemplate.AdminPasswordResetEmail, "Target", new SendAdminPasswordReset_RenderModel());
    public static InsertVariableTemplateFactory LoginBeginLoginWithMagicLinkEmail => new(BuiltInEmailTemplate.LoginBeginLoginWithMagicLinkEmail, "Target", new SendBeginLoginWithMagicLink_RenderModel());
    public static InsertVariableTemplateFactory LoginBeginForgotPasswordEmail => new(BuiltInEmailTemplate.LoginBeginForgotPasswordEmail, "Target", new SendBeginForgotPassword_RenderModel());
    public static InsertVariableTemplateFactory LoginCompletedForgotPasswordEmail => new(BuiltInEmailTemplate.LoginCompletedForgotPasswordEmail, "Target", new SendCompletedForgotPassword_RenderModel());
    public static InsertVariableTemplateFactory UserWelcomeEmail => new(BuiltInEmailTemplate.UserWelcomeEmail, "Target", new SendUserWelcomeEmail_RenderModel());
    public static InsertVariableTemplateFactory UserPasswordChangedEmail => new(BuiltInEmailTemplate.UserPasswordChangedEmail, "Target", new SendUserPasswordChanged_RenderModel());
    public static InsertVariableTemplateFactory UserPasswordResetEmail => new(BuiltInEmailTemplate.UserPasswordResetEmail, "Target", new SendUserPasswordReset_RenderModel());
    public static InsertVariableTemplateFactory CurrentOrganization => new("CurrentOrganization", "CurrentOrganization", new CurrentOrganization_RenderModel());
    public static InsertVariableTemplateFactory CurrentUser => new("CurrentUser", "CurrentUser", new CurrentUser_RenderModel());
    public static InsertVariableTemplateFactory ContentType => new("ContentType", "ContentType", new ContentType_RenderModel());
    public static InsertVariableTemplateFactory ContentItem => new("ContentItem", "Target", new ContentItem_RenderModel());
    public static InsertVariableTemplateFactory ContentItemListResult => new("ContentItemListResult", "Target", new ContentItemListResult_RenderModel());
    public static InsertVariableTemplateFactory NavigationMenu => new("Menu", "Menu", NavigationMenu_RenderModel.Empty());
    public static InsertVariableTemplateFactory NavigationMenuItem => new("MenuItem", "MenuItem", NavigationMenuItem_RenderModel.Empty());

    public static InsertVariableTemplateFactory LoginWithEmailAndPasswordPage => new(BuiltInWebTemplate.LoginWithEmailAndPasswordPage, "Target", new LoginSubmit_RenderModel());
    public static InsertVariableTemplateFactory LoginWithMagicLinkPage => new(BuiltInWebTemplate.LoginWithMagicLinkPage, "Target", new LoginSubmit_RenderModel());
    public static InsertVariableTemplateFactory LoginWithMagicLinkSentPage => new(BuiltInWebTemplate.LoginWithMagicLinkSentPage, "Target", new EmptyTarget_RenderModel());
    public static InsertVariableTemplateFactory ForgotPasswordPage => new(BuiltInWebTemplate.ForgotPasswordPage, "Target", new ForgotPasswordSubmit_RenderModel());
    public static InsertVariableTemplateFactory ForgotPasswordCompletePage => new(BuiltInWebTemplate.ForgotPasswordCompletePage, "Target", new ForgotPasswordCompleteSubmit_RenderModel());
    public static InsertVariableTemplateFactory ForgotPasswordResetLinkSentPage => new(BuiltInWebTemplate.ForgotPasswordResetLinkSentPage, "Target", new EmptyTarget_RenderModel());
    public static InsertVariableTemplateFactory ForgotPasswordSuccessPage => new(BuiltInWebTemplate.ForgotPasswordSuccessPage, "Target", new EmptyTarget_RenderModel());
    public static InsertVariableTemplateFactory ChangeProfilePage => new(BuiltInWebTemplate.ChangeProfilePage, "Target", new ChangeProfileSubmit_RenderModel());
    public static InsertVariableTemplateFactory ChangePasswordPage => new(BuiltInWebTemplate.ChangePasswordPage, "Target", new ChangeProfileSubmit_RenderModel());
    public static InsertVariableTemplateFactory UserRegistrationForm => new(BuiltInWebTemplate.UserRegistrationForm, "Target", new CreateUserSubmit_RenderModel());
    public static InsertVariableTemplateFactory UserRegistrationFormSuccess => new(BuiltInWebTemplate.UserRegistrationFormSuccess, "Target", new EmptyTarget_RenderModel());

    public static InsertVariableTemplateFactory Error403 => new(BuiltInWebTemplate.Error403, "Target", new GenericError_RenderModel());
    public static InsertVariableTemplateFactory Error404 => new(BuiltInWebTemplate.Error404, "Target", new GenericError_RenderModel());
    public static InsertVariableTemplateFactory Error500 => new(BuiltInWebTemplate.Error500, "Target", new GenericError_RenderModel());
    public static InsertVariableTemplateFactory _Layout => new(BuiltInWebTemplate._Layout, "Target", new GenericError_RenderModel());
    public static InsertVariableTemplateFactory _LoginLayout => new(BuiltInWebTemplate._LoginLayout, "Target", new GenericError_RenderModel());

    public string DeveloperName { get; private set; } = string.Empty;
    public string VariableCategoryName { get; private set;} = string.Empty;
    public IInsertTemplateVariable TemplateInfo { get; private set; } = null;

    public static IEnumerable<InsertVariableTemplateFactory> Templates
    {
        get
        {
            yield return Request;

            yield return AdminWelcomeEmail;
            yield return AdminPasswordChangedEmail;
            yield return AdminPasswordResetEmail;
            yield return LoginBeginLoginWithMagicLinkEmail;
            yield return LoginBeginForgotPasswordEmail;
            yield return LoginCompletedForgotPasswordEmail;
            yield return UserWelcomeEmail;
            yield return UserPasswordChangedEmail;
            yield return UserPasswordResetEmail;

            yield return CurrentOrganization;
            yield return CurrentUser;
            yield return ContentType;
            yield return ContentItem;
            yield return ContentItemListResult;

            yield return LoginWithEmailAndPasswordPage;
            yield return LoginWithMagicLinkPage;
            yield return LoginWithMagicLinkSentPage;
            yield return ForgotPasswordPage;
            yield return ForgotPasswordCompletePage;
            yield return ForgotPasswordResetLinkSentPage;
            yield return ForgotPasswordSuccessPage;
            yield return ChangeProfilePage;
            yield return ChangePasswordPage;
            yield return UserRegistrationForm;
            yield return UserRegistrationFormSuccess;

            yield return Error403;
            yield return Error404;
            yield return Error500;
            yield return _Layout;
            yield return _LoginLayout;
        }
    }
}
