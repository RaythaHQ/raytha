using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.ContentTypes.Commands;

public class EditContentType
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string LabelPlural { get; init; } = null!;
        public string LabelSingular { get; init; } = null!;
        public string Description { get; init; } = null!;
        public string DefaultRouteTemplate { get; init; } = null!;
        public ShortGuid PrimaryFieldId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.LabelPlural).NotEmpty();
            RuleFor(x => x.LabelSingular).NotEmpty();
            RuleFor(x => x.PrimaryFieldId).NotEmpty();
            RuleFor(x => x.DefaultRouteTemplate).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.ContentTypes.FirstOrDefault(p => p.Id == request.Id.Guid);
                if (entity == null)
                    throw new NotFoundException("Content Type", request.Id);

                var newPrimaryField = db.ContentTypeFields.FirstOrDefault(p => p.Id == request.PrimaryFieldId.Guid);
                if (newPrimaryField == null)
                    return;

                if (newPrimaryField.FieldType.DeveloperName != BaseFieldType.SingleLineText)
                {
                    context.AddFailure("PrimaryFieldId", "Primary field must be of type Single Line Text.");
                    return;
                }

                if (request.DefaultRouteTemplate.IsProtectedRoutePath())
                {
                    context.AddFailure("DefaultRouteTemplate", "Default route path cannot begin with a protected path.");
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
            var entity = _db.ContentTypes.First(p => p.Id == request.Id.Guid);

            entity.LabelPlural = request.LabelPlural;
            entity.LabelSingular = request.LabelSingular;
            entity.Description = request.Description;
            entity.PrimaryFieldId = request.PrimaryFieldId;
            entity.DefaultRouteTemplate = request.DefaultRouteTemplate;

            _db.ContentTypes.Update(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
