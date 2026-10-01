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

namespace Raytha.Application.ContentItems.Commands;

/// <summary>
/// Moves the listed content items of one content type into trash. Ids that are not items of
/// that type, and an empty list, are rejected. The home page blocks the whole call.
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
        public IReadOnlyList<ShortGuid> Ids { get; init; } = [];
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

                        var ids = request.Ids?.Select(id => id.Guid).Distinct().ToArray() ?? [];
                        if (ids.Length == 0 || ids.Any(id => id == Guid.Empty))
                        {
                            context.AddFailure("Ids", "Provide at least one content item id.");
                            return;
                        }

                        var found = db.ContentItems.Count(p =>
                            p.ContentTypeId == request.ContentTypeId.Guid && ids.Contains(p.Id)
                        );
                        if (found != ids.Length)
                        {
                            context.AddFailure(
                                "Ids",
                                "One or more content items were not found on this content type."
                            );
                            return;
                        }

                        var homePageId = db.OrganizationSettings.Select(p => p.HomePageId).First();
                        if (homePageId != null && ids.Contains(homePageId.Value))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot delete the home page. Change the home page first and then try again."
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

            var ids = request.Ids.Select(id => id.Guid).Distinct().ToArray();
            var items = await _db
                .ContentItems.Include(p => p.Route)
                .Where(p => p.ContentTypeId == request.ContentTypeId.Guid && ids.Contains(p.Id))
                .ToListAsync(cancellationToken);
            if (items.Count != ids.Length)
                throw new NotFoundException("Content item", request.ContentTypeId);

            var itemIds = items.Select(p => p.Id).ToArray();
            var templateIdsByItem = (
                await _db
                    .WebTemplateContentItemRelations.Where(wtr => itemIds.Contains(wtr.ContentItemId))
                    .Select(wtr => new { wtr.ContentItemId, wtr.WebTemplateId })
                    .ToListAsync(cancellationToken)
            )
                .GroupBy(wtr => wtr.ContentItemId)
                .ToDictionary(g => g.Key, g => g.Select(wtr => wtr.WebTemplateId).ToArray());

            foreach (var item in items)
            {
                templateIdsByItem.TryGetValue(item.Id, out var templateIds);

                _db.DeletedContentItems.Add(
                    new DeletedContentItem
                    {
                        _PublishedContent = item._PublishedContent,
                        ContentTypeId = item.ContentTypeId,
                        OriginalContentItemId = item.Id,
                        PrimaryField = PrimaryFieldValue(item, primaryField),
                        RoutePath = item.Route.Path,
                        WebTemplateIdsJson = JsonSerializer.Serialize(templateIds ?? []),
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
            if (string.IsNullOrWhiteSpace(item._PublishedContent))
                return "N/A";

            try
            {
                using var document = JsonDocument.Parse(item._PublishedContent);
                if (
                    document.RootElement.ValueKind != JsonValueKind.Object
                    || !document.RootElement.TryGetProperty(primaryField.DeveloperName, out var value)
                    || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                )
                {
                    return "N/A";
                }

                var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
                return string.IsNullOrEmpty(text) ? "N/A" : text;
            }
            catch (JsonException)
            {
                return "N/A";
            }
        }
    }
}
