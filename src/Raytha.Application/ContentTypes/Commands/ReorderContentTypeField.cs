using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentTypes.Commands;

public class ReorderContentTypeField
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public int NewFieldOrder { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.NewFieldOrder).Must((request, newFieldOrder) =>
            {
                var entity = db.ContentTypeFields
                    .Include(p => p.ContentType)
                    .ThenInclude(p => p.ContentTypeFields)
                    .FirstOrDefault(p => p.Id == request.Id.Guid);

                if (entity == null)
                    throw new NotFoundException("Content Type Field", request.Id);

                return !(request.NewFieldOrder <= 0 || request.NewFieldOrder > entity.ContentType.ContentTypeFields.Count());

            }).WithMessage(x => $"Invalid field order: {x.NewFieldOrder}");
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
            var entity = _db.ContentTypeFields
                .Include(p => p.ContentType)
                .ThenInclude(p => p.ContentTypeFields)
                .First(p => p.Id == request.Id.Guid);

            int originalFieldOrder = entity.FieldOrder;
            if (originalFieldOrder == request.NewFieldOrder)
                return new CommandResponseDto<ShortGuid>(entity.Id);

            var fieldsToUpdate = new List<ContentTypeField>();

            entity.FieldOrder = request.NewFieldOrder;
            fieldsToUpdate.Add(entity);

            if (request.NewFieldOrder < originalFieldOrder)
            {
                foreach (var customField in entity.ContentType.ContentTypeFields
                            .Where(p => p.Id != entity.Id &&
                                   p.FieldOrder >= request.NewFieldOrder &&
                                   p.FieldOrder < originalFieldOrder))
                {
                    customField.FieldOrder += 1;
                    fieldsToUpdate.Add(customField);
                }
            }
            else
            {
                foreach (var customField in entity.ContentType.ContentTypeFields
                            .Where(p => p.Id != entity.Id &&
                                   p.FieldOrder <= request.NewFieldOrder &&
                                   p.FieldOrder > originalFieldOrder))
                {
                    customField.FieldOrder -= 1;
                    fieldsToUpdate.Add(customField);
                }
            }

            _db.ContentTypeFields.UpdateRange(fieldsToUpdate);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
