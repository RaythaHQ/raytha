using CSharpVitamins;
using MediatR;
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
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entity = _db.DeletedContentItems
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content Item", request.Id);

            Guid templateId = Guid.Empty;

            var templateExists = _db.WebTemplates.FirstOrDefault(p => p.Id == entity.WebTemplateId);
            if (templateExists != null)
            {
                templateId = templateExists.Id;
            }
            else
            {
                templateId = _db.WebTemplates.First(p => p.DeveloperName == BuiltInWebTemplate.ContentItemDetailViewPage).Id;
            }

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
                WebTemplateId = templateId,
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

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.OriginalContentItemId);
        }
    }
}
