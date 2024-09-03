using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.ContentTypes.Commands;

public class CreateContentTypeField
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string FieldType { get; init; } = null!;
        public string DeveloperName { get; init; } = null!;
        public string Label { get; init; } = null!;
        public bool IsRequired { get; init; }
        public string Description { get; init; } = null!;
        public ShortGuid ContentTypeId { get; init; }
        public IEnumerable<ContentTypeFieldChoiceInputDto> Choices { get; init; } = new List<ContentTypeFieldChoiceInputDto>();
        public ShortGuid? RelatedContentTypeId { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.DeveloperName).Must(StringExtensions.IsValidDeveloperName).WithMessage("Invalid developer name.");
            RuleFor(x => x.ContentTypeId).NotEmpty();
            RuleFor(x => x.FieldType).NotEmpty();
            RuleFor(x => x.Choices).NotEmpty().When(p => BaseFieldType.From(p.FieldType).HasChoices);
            RuleFor(x => x.RelatedContentTypeId).NotEmpty().When(p => p.FieldType == BaseFieldType.OneToOneRelationship);
            RuleFor(x => x.DeveloperName).Must(p => NotReservedFieldName(p)).WithMessage($"Reserved word - cannot be used as a developer name");
            RuleFor(x => x).Custom((request, context) =>
            {
                var contentTypeField = db.ContentTypeFields
                    .IgnoreQueryFilters()
                    .Where(ctf => ctf.ContentTypeId == request.ContentTypeId.Guid && ctf.DeveloperName == request.DeveloperName.ToDeveloperName())
                    .Select(ctf => new { ctf.IsDeleted })
                    .FirstOrDefault();

                if (contentTypeField != null && !contentTypeField.IsDeleted)
                {
                    context.AddFailure("DeveloperName", "Another field with that developer name already exists.");
                    return;
                }
                
                if (contentTypeField != null && contentTypeField.IsDeleted)
                {
                    context.AddFailure("DeveloperName", "A previously deleted field has already used that developer name. You must choose another one.");
                    return;
                }

                if (BaseFieldType.From(request.FieldType).HasChoices)
                {
                    var choiceDeveloperNames = request.Choices.Select(p => p.DeveloperName.ToDeveloperName());
                    string dupes = string.Empty;
                    var foundDuplicates = choiceDeveloperNames
                        .GroupBy(i => i)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key);

                    dupes = string.Join("; ", foundDuplicates);

                    if (foundDuplicates.Any())
                    {
                        context.AddFailure("Choices", $"Your choices must have unique developer names. Duplicate developer names found: {dupes}");
                        return;
                    }

                    if (choiceDeveloperNames.Any(p => string.IsNullOrWhiteSpace(p)))
                    {
                        context.AddFailure("Choices", "Developer names cannot be null or empty.");
                        return;
                    }

                    if (request.Choices.Select(p => p.Label).Any(c => string.IsNullOrWhiteSpace(c)))
                    {
                        context.AddFailure("Choices", "Labels cannot be null or empty.");
                        return;
                    }
                }

                if (request.FieldType == BaseFieldType.OneToOneRelationship && request.RelatedContentTypeId.HasValue && !db.ContentTypes.Any(p => p.Id == request.RelatedContentTypeId.Value.Guid))
                {
                    context.AddFailure("RelatedContentTypeId", $"{request.RelatedContentTypeId} did not match any existing content type.");
                    return;
                }
            });
        }

        public bool NotReservedFieldName(string name)
        {
            return !BuiltInContentTypeField.ReservedContentTypeFields.Any(p => p == name.ToLower());
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
            var ordinal = _db.ContentTypeFields.Count(p => p.ContentTypeId == request.ContentTypeId.Guid);
            var fieldType = BaseFieldType.From(request.FieldType);

            IEnumerable<ContentTypeFieldChoice> choices = new ContentTypeFieldChoice[0];
            if (fieldType.HasChoices)
            {
                choices = request.Choices.Select(p => new ContentTypeFieldChoice { DeveloperName = p.DeveloperName.ToDeveloperName(), Disabled = p.Disabled, Label = p.Label });
            }

            Guid? relatedContentTypeId = request.RelatedContentTypeId == ShortGuid.Empty || request.RelatedContentTypeId == null ? null : request.RelatedContentTypeId.Value.Guid;
            var entity = new ContentTypeField
            {
                Id = Guid.NewGuid(),
                Label = request.Label,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                FieldType = fieldType,
                ContentTypeId = request.ContentTypeId,
                FieldOrder = ordinal + 1,
                Choices = choices,
                IsRequired = request.IsRequired,
                Description = request.Description,
                RelatedContentTypeId = relatedContentTypeId
            };
            _db.ContentTypeFields.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
