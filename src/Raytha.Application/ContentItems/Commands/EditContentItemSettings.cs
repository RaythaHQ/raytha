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
using Raytha.Domain.Events;

namespace Raytha.Application.ContentItems.Commands;

[WebhookEvent("content_item.settings_updated", DisplayName = "Content item settings updated", Group = "Content")]
public class EditContentItemSettings
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid TemplateId { get; init; }
        public string RoutePath { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, IContentTypeInRoutePath contentTypeInRoutePath)
        {
            RuleFor(x => x.RoutePath).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (request.Id == ShortGuid.Empty)
                        {
                            context.AddFailure(Constants.VALIDATION_SUMMARY, "Id is required.");
                            return;
                        }

                        if (request.TemplateId == ShortGuid.Empty)
                        {
                            context.AddFailure("TemplateId", "Template is required.");
                            return;
                        }

                        var entity = db
                            .ContentItems.Include(p => p.ContentType)
                            .FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("Content Item", request.Id);

                        contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(
                            entity.ContentType.DeveloperName
                        );

                        var activeThemeId = db
                            .OrganizationSettings.Select(os => os.ActiveThemeId)
                            .First();

                        var templateAccessToModelDefinitions = db
                            .WebTemplates.Include(wt => wt.TemplateAccessToModelDefinitions)
                            .Where(wt =>
                                wt.ThemeId == activeThemeId && wt.Id == request.TemplateId.Guid
                            )
                            .Select(wt => wt.TemplateAccessToModelDefinitions)
                            .FirstOrDefault();

                        if (templateAccessToModelDefinitions == null)
                        {
                            throw new NotFoundException("Template", request.TemplateId);
                        }

                        if (
                            !templateAccessToModelDefinitions.Any(p =>
                                p.ContentTypeId == entity.ContentType.Id
                            )
                        )
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "This template does not have access to this model definition."
                            );
                            return;
                        }

                        var path = RoutePaths.Normalize(request.RoutePath);
                        var problem = RoutePaths.Problem(db, path, entity.RouteId);
                        if (problem != null)
                        {
                            context.AddFailure("RoutePath", problem);
                            return;
                        }
                        if (RoutePaths.IsTaken(db, path, entity.RouteId))
                        {
                            context.AddFailure(
                                "RoutePath",
                                $"The route path {path} already exists."
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
            var entity = _db.ContentItems.Include(p => p.Route).First(p => p.Id == request.Id.Guid);

            entity.Route.Path = RoutePaths.Normalize(request.RoutePath);

            var activeThemeId = await _db
                .OrganizationSettings.Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var webTemplateContentRelation =
                await _db.WebTemplateContentItemRelations.FirstOrDefaultAsync(
                    wtr =>
                        wtr.ContentItemId == entity.Id && wtr.WebTemplate!.ThemeId == activeThemeId,
                    cancellationToken
                );

            if (webTemplateContentRelation == null)
            {
                webTemplateContentRelation = new WebTemplateContentItemRelation
                {
                    Id = Guid.NewGuid(),
                    ContentItemId = entity.Id,
                    WebTemplateId = request.TemplateId.Guid,
                };
                await _db.WebTemplateContentItemRelations.AddAsync(
                    webTemplateContentRelation,
                    cancellationToken
                );
            }
            else
            {
                webTemplateContentRelation.WebTemplateId = request.TemplateId.Guid;
            }

            entity.AddDomainEvent(new ContentItemUpdatedEvent(entity));
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
