using CSharpVitamins;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Domain.Common;
using Raytha.Domain.Entities;
using Raytha.Domain.Events;

namespace Raytha.Application.Admins.EventHandlers;

public class AdminCreatedEventHandler : INotificationHandler<AdminCreatedEvent>
{
    private readonly IEmailer _emailerService;
    private readonly IRaythaDbContext _db;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilderService;
    private readonly ICurrentOrganization _currentOrganization;

    public AdminCreatedEventHandler(
        ICurrentOrganization currentOrganization,
        IRaythaDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilderService
    )
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _relativeUrlBuilderService = relativeUrlBuilderService;
        _currentOrganization = currentOrganization;
    }

    public async ValueTask Handle(AdminCreatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.SendEmail)
        {
            EmailTemplate renderTemplate = _db.EmailTemplates.First(p =>
                p.DeveloperName == BuiltInEmailTemplate.AdminWelcomeEmail
            );

            SendAdminWelcomeEmail_RenderModel entity = new SendAdminWelcomeEmail_RenderModel
            {
                Id = (ShortGuid)notification.User.Id,
                FirstName = notification.User.FirstName,
                LastName = notification.User.LastName,
                EmailAddress = notification.User.EmailAddress,
                NewPassword = notification.NewPassword,
                LoginUrl = _relativeUrlBuilderService.AdminLoginUrl(),
                AuthenticationScheme = notification.User.AuthenticationScheme?.DeveloperName,
                LoginWithEmailAndPasswordIsEnabled =
                    _currentOrganization.EmailAndPasswordIsEnabledForAdmins,
                IsNewlyCreatedUser = notification.IsNewlyCreatedUser,
            };

            var wrappedModel = new Wrapper_RenderModel
            {
                CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                    _currentOrganization
                ),
                Target = entity,
            };

            string subject = _renderEngineService.RenderAsHtml(
                renderTemplate.Subject,
                wrappedModel
            );
            string content = _renderEngineService.RenderAsHtml(
                renderTemplate.Content,
                wrappedModel
            );
            var emailMessage = new EmailMessage
            {
                Content = content,
                To = new List<string> { entity.EmailAddress },
                Subject = subject,
            };
            _emailerService.SendEmail(emailMessage);
        }
    }
}
