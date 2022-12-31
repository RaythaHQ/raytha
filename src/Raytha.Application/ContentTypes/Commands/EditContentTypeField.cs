using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.ContentTypes.Commands;

public class EditContentTypeField
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string Label { get; init; } = null!;
        public IEnumerable<ContentTypeFieldChoiceInputDto> Choices { get; init; } = new List<ContentTypeFieldChoiceInputDto>();
        public bool IsRequired { get; init; }
        public string Description { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.ContentTypeFields.FirstOrDefault(p => p.Id == request.Id.Guid);
                if (entity == null)
                    throw new NotFoundException("Content Type Field", request.Id);

                if (BaseFieldType.From(entity.FieldType).HasChoices && !request.Choices.Any())
                {
                    context.AddFailure("Choices", "You must have at least one choice specified.");
                    return;
                }

                if (BaseFieldType.From(entity.FieldType).HasChoices)
                {
                    var choiceDeveloperNames = request.Choices.Select(p => p.DeveloperName.ToDeveloperName());
                    String dupes = string.Empty;
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
            var entity = _db.ContentTypeFields.First(p => p.Id == request.Id.Guid);

            if (BaseFieldType.From(entity.FieldType).HasChoices)
                entity.Choices = request.Choices.Select(p => new ContentTypeFieldChoice { DeveloperName = p.DeveloperName.ToDeveloperName(), Disabled = p.Disabled, Label = p.Label });

            entity.Label = request.Label;
            entity.Description = request.Description;
            entity.IsRequired = request.IsRequired;

            _db.ContentTypeFields.Update(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
