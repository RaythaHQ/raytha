using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentItems.Commands;

public class EditContentItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public bool SaveAsDraft { get; init; }
        public IDictionary<string, dynamic> Content { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.ContentItems
                    .Include(p => p.ContentType)
                    .ThenInclude(p => p.ContentTypeFields)
                    .FirstOrDefault(p => p.Id == request.Id.Guid);

                if (entity == null)
                    throw new NotFoundException("Content Item", request.Id);

                var contentTypeDefinition = entity.ContentType;
                if (contentTypeDefinition == null)
                    throw new NotFoundException("Content Type");

                foreach (var field in request.Content as IDictionary<string, dynamic>)
                {
                    var fieldDefinition = contentTypeDefinition.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == field.Key);
                    if (fieldDefinition == null)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, $"{field.Key} is not a recognized field for content type: {contentTypeDefinition.LabelSingular}");
                    }
                    else
                    {
                        if (fieldDefinition.IsRequired)
                        {
                            var fieldValue = fieldDefinition.FieldType.FieldValueFrom(field.Value);
                            if (!fieldValue.HasValue)
                            {
                                context.AddFailure(fieldDefinition.DeveloperName, $"'{fieldDefinition.Label}' field is required.");
                            }
                        }
                    }
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
            var entity = _db.ContentItems.First(p => p.Id == request.Id.Guid);

            if (request.SaveAsDraft)
            {
                entity.IsDraft = true;
            }
            else
            {
                entity.IsDraft = false;
                entity.IsPublished = true;
            }

            entity.DraftContent = request.Content;

            if (!entity.IsDraft)
            {     
                _db.ContentItemRevisions.Add(new ContentItemRevision
                {
                    ContentItemId = entity.Id,
                    PublishedContent = entity.PublishedContent
                });
                entity.PublishedContent = request.Content;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
