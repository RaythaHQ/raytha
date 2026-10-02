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

[WebhookEvent("content_item.updated", DisplayName = "Content item updated", Group = "Content")]
public class EditContentItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public bool SaveAsDraft { get; init; }
        public IDictionary<string, dynamic> Content { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, IContentTypeInRoutePath contentTypeInRoutePath)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db
                            .ContentItems.Include(p => p.ContentType)
                            .ThenInclude(p => p.ContentTypeFields)
                            .FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("Content Item", request.Id);

                        var contentTypeDefinition = entity.ContentType;
                        if (contentTypeDefinition == null)
                            throw new NotFoundException("Content Type");

                        contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(
                            entity.ContentType.DeveloperName
                        );

                        foreach (var field in request.Content as IDictionary<string, dynamic>)
                        {
                            var fieldDefinition =
                                contentTypeDefinition.ContentTypeFields.FirstOrDefault(p =>
                                    p.DeveloperName == field.Key
                                );
                            if (fieldDefinition == null)
                            {
                                context.AddFailure(
                                    Constants.VALIDATION_SUMMARY,
                                    $"{field.Key} is not a recognized field for content type: {contentTypeDefinition.LabelSingular}"
                                );
                            }
                            else
                            {
                                try
                                {
                                    var fieldValue = fieldDefinition.FieldType.FieldValueFrom(
                                        field.Value
                                    );
                                    if (
                                        !request.SaveAsDraft
                                        && fieldDefinition.IsRequired
                                        && !fieldValue.HasValue
                                    )
                                    {
                                        context.AddFailure(
                                            fieldDefinition.DeveloperName,
                                            $"'{fieldDefinition.Label}' field is required."
                                        );
                                    }
                                    foreach (
                                        var error in FieldDefinitionValues.RowErrors(
                                            fieldDefinition,
                                            fieldValue,
                                            !request.SaveAsDraft
                                        )
                                    )
                                    {
                                        context.AddFailure(fieldDefinition.DeveloperName, error);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    context.AddFailure(
                                        fieldDefinition.DeveloperName,
                                        $"'{fieldDefinition.Label}' is an invalid format."
                                    );
                                }
                            }
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
            var entity = _db
                .ContentItems.Include(p => p.CreatorUser)
                .Include(p => p.LastModifierUser)
                .Include(p => p.ContentType)
                .ThenInclude(p => p.ContentTypeFields)
                .Include(p => p.Route)
                .First(p => p.Id == request.Id.Guid);
            var content = FieldDefinitionValues.ToStoredContent(
                entity.ContentType!.ContentTypeFields,
                request.Content
            );

            if (request.SaveAsDraft)
            {
                entity.IsDraft = true;
            }
            else
            {
                entity.IsDraft = false;
                entity.IsPublished = true;
            }

            entity.DraftContent = content;

            if (!entity.IsDraft)
            {
                _db.ContentItemRevisions.Add(
                    new ContentItemRevision
                    {
                        ContentItemId = entity.Id,
                        PublishedContent = entity.PublishedContent,
                    }
                );
                entity.PublishedContent = content;
            }
            entity.AddDomainEvent(new ContentItemUpdatedEvent(entity));

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
