using System.Text.Json;
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
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Application.ContentItems.Commands;

/// <summary>
/// Moves every content item of one content type into trash in one call. The home page item
/// blocks the whole call, the same way deleting that item alone does.
/// </summary>
[WebhookEvent(
    "content_item.bulk_deleted",
    DisplayName = "Content items deleted",
    Group = "Content"
)]
public class DeleteContentItems
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid ContentTypeId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (!db.ContentTypes.Any(p => p.Id == request.ContentTypeId.Guid))
                            throw new NotFoundException("Content type", request.ContentTypeId);

                        var settings = db.OrganizationSettings.First();
                        if (
                            settings.HomePageType == Route.CONTENT_ITEM_TYPE
                            && settings.HomePageId != null
                            && db.ContentItems.Any(p =>
                                p.Id == settings.HomePageId
                                && p.ContentTypeId == request.ContentTypeId.Guid
                            )
                        )
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "One of these items is the home page. Set a different home page first."
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
            var contentType = await _db
                .ContentTypes.Include(p => p.ContentTypeFields)
                .FirstAsync(p => p.Id == request.ContentTypeId.Guid, cancellationToken);
            var primaryField = contentType.ContentTypeFields.First(p =>
                p.Id == contentType.PrimaryFieldId
            );

            var items = await _db
                .ContentItems.Include(p => p.Route)
                .Where(p => p.ContentTypeId == request.ContentTypeId.Guid)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                var templateIds = await _db
                    .WebTemplateContentItemRelations.Where(wtr => wtr.ContentItemId == item.Id)
                    .Select(wtr => wtr.WebTemplateId)
                    .ToArrayAsync(cancellationToken);

                _db.DeletedContentItems.Add(
                    new DeletedContentItem
                    {
                        _PublishedContent = item._PublishedContent,
                        ContentTypeId = item.ContentTypeId,
                        OriginalContentItemId = item.Id,
                        PrimaryField = PrimaryFieldValue(item, primaryField),
                        RoutePath = item.Route.Path,
                        WebTemplateIdsJson = JsonSerializer.Serialize(templateIds),
                    }
                );
                _db.Routes.Remove(item.Route);
                _db.ContentItems.Remove(item);
                item.AddDomainEvent(new ContentItemDeletedEvent(item));
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(request.ContentTypeId);
        }

        private static string PrimaryFieldValue(ContentItem item, ContentTypeField primaryField)
        {
            try
            {
                var published = (Dictionary<string, dynamic>)item.PublishedContent;
                if (
                    published != null
                    && published.ContainsKey(primaryField.DeveloperName)
                )
                {
                    StringFieldValue value = primaryField.FieldType.FieldValueFrom(
                        published[primaryField.DeveloperName]
                    );
                    return value.Value;
                }
            }
            catch (InvalidCastException)
            {
                return "N/A";
            }
            catch (JsonException)
            {
                return "N/A";
            }

            return "N/A";
        }
    }
}
