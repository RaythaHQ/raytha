using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Views.Commands;

public class EditView
{
    public record Command : LoggableEntityRequest<ICommandResponseDto<ShortGuid>>
    {
        public string Label { get; init; } = null!;
        public string Description { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Label).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, ICommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<ICommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db.Views.FirstOrDefault(p => p.Id == request.Id.Guid);
            if (entity == null)
                throw new NotFoundException("View", request.Id);

            entity.Label = request.Label;
            entity.Description = request.Description;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
