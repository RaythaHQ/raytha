using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.Events;

namespace Raytha.Application.ContentItems.Commands;

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
            RuleFor(x => x).Custom((request, context) =>
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

                var entity = db.ContentItems
                    .Include(p => p.ContentType)
                    .FirstOrDefault(p => p.Id == request.Id.Guid);

                if (entity == null)
                    throw new NotFoundException("Content Item", request.Id);

                contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(entity.ContentType.DeveloperName);

                var activeThemeId = db.OrganizationSettings
                    .Select(os => os.ActiveThemeId)
                    .First();

                var templateAccessToModelDefinitions = db.WebTemplates
                    .Include(wt => wt.TemplateAccessToModelDefinitions)
                    .Where(wt => wt.ThemeId == activeThemeId && wt.Id == request.TemplateId.Guid)
                    .Select(wt => wt.TemplateAccessToModelDefinitions)
                    .FirstOrDefault();

                if (templateAccessToModelDefinitions == null)
                {
                    throw new NotFoundException("Template", request.TemplateId);
                }

                if (!templateAccessToModelDefinitions.Any(p => p.ContentTypeId == entity.ContentType.Id))
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "This template does not have access to this model definition.");
                    return;
                }

                var slugifiedPath = request.RoutePath.ToUrlSlug();
                if (string.IsNullOrWhiteSpace(slugifiedPath))
                {
                    context.AddFailure("RoutePath", "Invalid route path. Must be letters, numbers, and dashes");
                    return;
                }
                if (slugifiedPath.Length > 200)
                {
                    context.AddFailure("RoutePath", "Invalid route path. Must be less than 200 characters");
                    return;
                }
                var routePathExists = db.Routes.Any(p => p.Path.ToLower() == slugifiedPath && p.ContentItemId != request.Id.Guid);
                if (routePathExists)
                {
                    context.AddFailure("RoutePath", $"The route path {request.RoutePath.ToUrlSlug()} already exists.");
                    return;
                }
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entity = _db.ContentItems
                .Include(p => p.Route)
                .First(p => p.Id == request.Id.Guid);

            entity.Route.Path = request.RoutePath.ToUrlSlug();

            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var webTemplateContentRelation = await _db.WebTemplateContentItemRelations
                .FirstAsync(wtr => wtr.ContentItemId == entity.Id && wtr.WebTemplate!.ThemeId == activeThemeId, cancellationToken);

            webTemplateContentRelation.WebTemplateId = request.TemplateId.Guid;
            _db.WebTemplateContentItemRelations.Update(webTemplateContentRelation);

            entity.AddDomainEvent(new ContentItemUpdatedEvent(entity));
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}