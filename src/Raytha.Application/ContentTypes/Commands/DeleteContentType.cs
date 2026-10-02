using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.Webhooks;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentTypes.Commands;

[WebhookEvent("content_type.deleted", DisplayName = "Content type deleted", Group = "Content types")]
public class DeleteContentType
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db.ContentTypes.FirstOrDefault(p => p.Id == request.Id.Guid);
                        if (entity == null)
                            throw new NotFoundException("Content type", request.Id);

                        var referencedByAnotherType = db.ContentTypeFields.Any(f =>
                            f.RelatedContentTypeId == request.Id.Guid
                            && f.ContentTypeId != request.Id.Guid
                        );
                        if (referencedByAnotherType)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Another content type has a relationship field pointing at this one. Remove that field first."
                            );
                            return;
                        }

                        var settings = db.OrganizationSettings.First();
                        if (settings.HomePageId == null)
                            return;

                        var homePageIsAnItem =
                            settings.HomePageType == Route.CONTENT_ITEM_TYPE
                            && db.ContentItems.Any(p =>
                                p.Id == settings.HomePageId && p.ContentTypeId == request.Id.Guid
                            );
                        var homePageIsAView =
                            settings.HomePageType == Route.VIEW_TYPE
                            && db.Views.Any(p =>
                                p.Id == settings.HomePageId && p.ContentTypeId == request.Id.Guid
                            );
                        if (homePageIsAnItem || homePageIsAView)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "This content type owns the home page. Set a different home page first."
                            );
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
            var contentTypeId = request.Id.Guid;

            var items = await _db
                .ContentItems.Include(p => p.Route)
                .Where(p => p.ContentTypeId == contentTypeId)
                .ToListAsync(cancellationToken);
            _db.Routes.RemoveRange(items.Select(p => p.Route).Where(p => p != null)!);
            _db.ContentItems.RemoveRange(items);

            var views = await _db
                .Views.Include(p => p.Route)
                .Include(p => p.UserFavorites)
                .Where(p => p.ContentTypeId == contentTypeId)
                .ToListAsync(cancellationToken);
            foreach (var view in views)
                view.UserFavorites.Clear();
            _db.Routes.RemoveRange(views.Select(p => p.Route).Where(p => p != null)!);
            _db.Views.RemoveRange(views);

            var access = await _db
                .WebTemplateAccessToModelDefinitions.Where(p => p.ContentTypeId == contentTypeId)
                .ToListAsync(cancellationToken);
            _db.WebTemplateAccessToModelDefinitions.RemoveRange(access);

            var trash = await _db
                .DeletedContentItems.Where(p => p.ContentTypeId == contentTypeId)
                .ToListAsync(cancellationToken);
            _db.DeletedContentItems.RemoveRange(trash);

            var permissions = await _db
                .DbContext.Set<ContentTypeRolePermission>()
                .Where(p => p.ContentTypeId == contentTypeId)
                .ToListAsync(cancellationToken);
            _db.DbContext.RemoveRange(permissions);

            var fields = await _db
                .ContentTypeFields.IgnoreQueryFilters()
                .Where(p => p.ContentTypeId == contentTypeId && !p.IsDeleted)
                .ToListAsync(cancellationToken);
            _db.ContentTypeFields.RemoveRange(fields);

            var entity = await _db.ContentTypes.FirstAsync(
                p => p.Id == contentTypeId,
                cancellationToken
            );
            _db.ContentTypes.Remove(entity);

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
