using System.Text.Json;
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
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Application.ContentItems.Commands;

public class DeleteContentItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                var orgSettings = db.OrganizationSettings.First();

                if (orgSettings.HomePageId == request.Id.Guid)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "You cannot delete the home page. Change the home page first and then try again.");
                    return;
                }
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _entityFrameworkDb;
        private readonly IRaythaDbJsonQueryEngine _db;
        private readonly IContentTypeInRoutePath _contentTypeInRoutePath;
        public Handler(IRaythaDbContext entityFrameworkDb, IRaythaDbJsonQueryEngine db, IContentTypeInRoutePath contentTypeInRoutePath)
        {
            _entityFrameworkDb = entityFrameworkDb;
            _db = db;
            _contentTypeInRoutePath = contentTypeInRoutePath;
        }
        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entityFromRaythaEngine = _db
                .FirstOrDefault(request.Id.Guid);

            if (entityFromRaythaEngine == null)
                throw new NotFoundException("Content item", request.Id);

            var contentType = _entityFrameworkDb.ContentTypes.Include(p => p.ContentTypeFields).First(p => p.Id == entityFromRaythaEngine.ContentTypeId);

            _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(contentType.DeveloperName);

            var primaryField = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId);

            var publishedContent = (Dictionary<string, dynamic>)entityFromRaythaEngine.PublishedContent;
            string primaryFieldValue = string.Empty;

            if (publishedContent != null && publishedContent.ContainsKey(primaryField.DeveloperName))
            {
                StringFieldValue stringFieldValue = primaryField.FieldType.FieldValueFrom(entityFromRaythaEngine.PublishedContent[primaryField.DeveloperName]);
                primaryFieldValue = stringFieldValue.Value;
            }
            else
            {
                primaryFieldValue = "N/A";
            }

            var entityToDelete = _entityFrameworkDb.ContentItems
                .Include(p => p.Route)
                .First(p => p.Id == request.Id.Guid);

            var webTemplateIds = await _entityFrameworkDb.WebTemplateContentItemRelations
                .Where(wtr => wtr.ContentItemId == request.Id.Guid)
                .Select(wtr => wtr.WebTemplateId)
                .ToArrayAsync(cancellationToken);

            var trashEntity = new DeletedContentItem
            {
                _PublishedContent = entityToDelete._PublishedContent,
                ContentTypeId = entityToDelete.ContentTypeId,
                OriginalContentItemId = request.Id.Guid,
                PrimaryField = primaryFieldValue,
                RoutePath = entityToDelete.Route.Path,
                WebTemplateIdsJson = JsonSerializer.Serialize(webTemplateIds),
            };

            _entityFrameworkDb.DeletedContentItems.Add(trashEntity);
            _entityFrameworkDb.ContentItems.Remove(entityToDelete);
            _entityFrameworkDb.Routes.Remove(entityToDelete.Route);

            entityToDelete.AddDomainEvent(new ContentItemDeletedEvent(entityToDelete));

            await _entityFrameworkDb.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
