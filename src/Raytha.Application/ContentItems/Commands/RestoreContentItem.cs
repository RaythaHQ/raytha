using System.Text.Json;
using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentItems.Commands;

public class RestoreContentItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IContentTypeInRoutePath _contentTypeInRoutePath;
        public Handler(IRaythaDbContext db, IContentTypeInRoutePath contentTypeInRoutePath)
        {
            _db = db;
            _contentTypeInRoutePath = contentTypeInRoutePath;
        }
        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entity = _db.DeletedContentItems
                .Include(p => p.ContentType)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content Item", request.Id);

            _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(entity.ContentType.DeveloperName);

            string path = string.Empty;
            var routePathExists = _db.Routes.FirstOrDefault(p => p.Path.ToLower() == entity.RoutePath);
            if (routePathExists != null)
            {
                path = $"{entity.ContentType.DeveloperName}/{(ShortGuid)entity.Id}";
            }
            else
            {
                path = entity.RoutePath;
            }

            var restoredEntity = new ContentItem
            {
                _PublishedContent = entity._PublishedContent,
                _DraftContent = entity._PublishedContent,
                ContentTypeId = entity.ContentTypeId,
                Id = entity.OriginalContentItemId,
                IsDraft = false,
                IsPublished = false,
                Route = new Route
                {
                    Path = path,
                    ContentItemId = entity.OriginalContentItemId
                }
            };
            _db.ContentItems.Add(restoredEntity);
            _db.DeletedContentItems.Remove(entity);

            var webTemplateIdsFromJson = JsonSerializer.Deserialize<IEnumerable<Guid>>(entity.WebTemplateIdsJson)!;

            var webTemplateInfos = await _db.WebTemplates
                .Where(wt => webTemplateIdsFromJson.Contains(wt.Id))
                .Select(wt => new
                {
                    wt.Id,
                    wt.ThemeId,
                }).ToListAsync(cancellationToken);

            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            if (!webTemplateInfos.Any(wt => wt.ThemeId == activeThemeId))
            {
                var defaultWebTemplate = await _db.WebTemplates
                    .Where(wt => wt.ThemeId == activeThemeId && wt.DeveloperName == BuiltInWebTemplate.ContentItemDetailViewPage.DeveloperName)
                    .Select(wt => new
                    {
                        wt.Id,
                        wt.ThemeId,
                    }).FirstAsync(cancellationToken);

                webTemplateInfos.Add(defaultWebTemplate);
            }

            await _db.WebTemplateContentItemRelations.AddRangeAsync(webTemplateInfos.Select(wt => new WebTemplateContentItemRelation
            {
                Id = Guid.NewGuid(),
                ContentItemId = restoredEntity.Id,
                WebTemplateId = wt.Id,
            }), cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.OriginalContentItemId);
        }
    }
}
