using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Admins.Commands;

public class DeleteAdmin
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(ICurrentUser currentUser)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (request.Id == currentUser.UserId)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot remove your own account."
                            );
                            return;
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db.Users.FirstOrDefault(p => p.Id == request.Id.Guid && p.IsAdmin);
            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            // Null out foreign key references to prevent constraint violations
            var contentItems = _db.ContentItems.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (contentItems.Any())
            {
                foreach (var contentItem in contentItems)
                {
                    contentItem.CreatorUserId = null;
                    contentItem.LastModifierUserId = null;
                }
            }
            _db.ContentItems.UpdateRange(contentItems);

            var contentItemRevisions = _db.ContentItemRevisions.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (contentItemRevisions.Any())
            {
                foreach (var contentItemRevision in contentItemRevisions)
                {
                    contentItemRevision.CreatorUserId = null;
                    contentItemRevision.LastModifierUserId = null;
                }
            }
            _db.ContentItemRevisions.UpdateRange(contentItemRevisions);

            var views = _db.Views.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (views.Any())
            {
                foreach (var view in views)
                {
                    view.CreatorUserId = null;
                    view.LastModifierUserId = null;
                }
            }
            _db.Views.UpdateRange(views);

            var mediaItems = _db.MediaItems.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (mediaItems.Any())
            {
                foreach (var mediaItem in mediaItems)
                {
                    mediaItem.CreatorUserId = null;
                    mediaItem.LastModifierUserId = null;
                }
            }
            _db.MediaItems.UpdateRange(mediaItems);

            var themes = _db.Themes.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (themes.Any())
            {
                foreach (var theme in themes)
                {
                    theme.CreatorUserId = null;
                    theme.LastModifierUserId = null;
                }
            }
            _db.Themes.UpdateRange(themes);

            var webTemplates = _db.WebTemplates.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (webTemplates.Any())
            {
                foreach (var webTemplate in webTemplates)
                {
                    webTemplate.CreatorUserId = null;
                    webTemplate.LastModifierUserId = null;
                }
            }
            _db.WebTemplates.UpdateRange(webTemplates);

            var raythaFunctions = _db.RaythaFunctions.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (raythaFunctions.Any())
            {
                foreach (var raythaFunction in raythaFunctions)
                {
                    raythaFunction.CreatorUserId = null;
                    raythaFunction.LastModifierUserId = null;
                }
            }
            _db.RaythaFunctions.UpdateRange(raythaFunctions);

            var raythaFunctionRevisions = _db.RaythaFunctionRevisions.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (raythaFunctionRevisions.Any())
            {
                foreach (var raythaFunctionRevision in raythaFunctionRevisions)
                {
                    raythaFunctionRevision.CreatorUserId = null;
                    raythaFunctionRevision.LastModifierUserId = null;
                }
            }
            _db.RaythaFunctionRevisions.UpdateRange(raythaFunctionRevisions);

            var navigationMenus = _db.NavigationMenus.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (navigationMenus.Any())
            {
                foreach (var navigationMenu in navigationMenus)
                {
                    navigationMenu.CreatorUserId = null;
                    navigationMenu.LastModifierUserId = null;
                }
            }
            _db.NavigationMenus.UpdateRange(navigationMenus);

            var navigationMenuItems = _db.NavigationMenuItems.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (navigationMenuItems.Any())
            {
                foreach (var navigationMenuItem in navigationMenuItems)
                {
                    navigationMenuItem.CreatorUserId = null;
                    navigationMenuItem.LastModifierUserId = null;
                }
            }
            _db.NavigationMenuItems.UpdateRange(navigationMenuItems);

            var navigationMenuRevisions = _db.NavigationMenuRevisions.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (navigationMenuRevisions.Any())
            {
                foreach (var navigationMenuRevision in navigationMenuRevisions)
                {
                    navigationMenuRevision.CreatorUserId = null;
                    navigationMenuRevision.LastModifierUserId = null;
                }
            }
            _db.NavigationMenuRevisions.UpdateRange(navigationMenuRevisions);

            var emailTemplates = _db.EmailTemplates.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (emailTemplates.Any())
            {
                foreach (var emailTemplate in emailTemplates)
                {
                    emailTemplate.CreatorUserId = null;
                    emailTemplate.LastModifierUserId = null;
                }
            }
            _db.EmailTemplates.UpdateRange(emailTemplates);

            var userGroups = _db.UserGroups.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (userGroups.Any())
            {
                foreach (var userGroup in userGroups)
                {
                    userGroup.CreatorUserId = null;
                    userGroup.LastModifierUserId = null;
                }
            }
            _db.UserGroups.UpdateRange(userGroups);

            var contentTypes = _db.ContentTypes.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (contentTypes.Any())
            {
                foreach (var contentType in contentTypes)
                {
                    contentType.CreatorUserId = null;
                    contentType.LastModifierUserId = null;
                }
            }
            _db.ContentTypes.UpdateRange(contentTypes);

            var contentTypeFields = _db.ContentTypeFields.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (contentTypeFields.Any())
            {
                foreach (var contentTypeField in contentTypeFields)
                {
                    contentTypeField.CreatorUserId = null;
                    contentTypeField.LastModifierUserId = null;
                }
            }
            _db.ContentTypeFields.UpdateRange(contentTypeFields);

            var apiKeys = _db.ApiKeys.Where(p =>
                p.CreatorUserId == request.Id.Guid
            );

            if (apiKeys.Any())
            {
                foreach (var apiKey in apiKeys)
                {
                    apiKey.CreatorUserId = null;
                }
            }
            _db.ApiKeys.UpdateRange(apiKeys);

            var roles = _db.Roles.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (roles.Any())
            {
                foreach (var role in roles)
                {
                    role.CreatorUserId = null;
                    role.LastModifierUserId = null;
                }
            }
            _db.Roles.UpdateRange(roles);

            var authenticationSchemes = _db.AuthenticationSchemes.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (authenticationSchemes.Any())
            {
                foreach (var authenticationScheme in authenticationSchemes)
                {
                    authenticationScheme.CreatorUserId = null;
                    authenticationScheme.LastModifierUserId = null;
                }
            }
            _db.AuthenticationSchemes.UpdateRange(authenticationSchemes);

            var deletedContentItems = _db.DeletedContentItems.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (deletedContentItems.Any())
            {
                foreach (var deletedContentItem in deletedContentItems)
                {
                    deletedContentItem.CreatorUserId = null;
                    deletedContentItem.LastModifierUserId = null;
                }
            }
            _db.DeletedContentItems.UpdateRange(deletedContentItems);

            var verificationCodes = _db.VerificationCodes.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (verificationCodes.Any())
            {
                foreach (var verificationCode in verificationCodes)
                {
                    verificationCode.CreatorUserId = null;
                    verificationCode.LastModifierUserId = null;
                }
            }
            _db.VerificationCodes.UpdateRange(verificationCodes);

            var sitePages = _db.SitePages.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (sitePages.Any())
            {
                foreach (var sitePage in sitePages)
                {
                    sitePage.CreatorUserId = null;
                    sitePage.LastModifierUserId = null;
                }
            }
            _db.SitePages.UpdateRange(sitePages);

            var sitePageRevisions = _db.SitePageRevisions.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (sitePageRevisions.Any())
            {
                foreach (var sitePageRevision in sitePageRevisions)
                {
                    sitePageRevision.CreatorUserId = null;
                    sitePageRevision.LastModifierUserId = null;
                }
            }
            _db.SitePageRevisions.UpdateRange(sitePageRevisions);

            var users = _db.Users.Where(p =>
                p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid
            );

            if (users.Any())
            {
                foreach (var user in users)
                {
                    user.CreatorUserId = null;
                    user.LastModifierUserId = null;
                }
            }
            _db.Users.UpdateRange(users);

            _db.Users.Remove(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
