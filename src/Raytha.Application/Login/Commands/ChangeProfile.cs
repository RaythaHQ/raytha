using MediatR;
using FluentValidation;
using Raytha.Application.Common.Models;
using CSharpVitamins;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Exceptions;

namespace Raytha.Application.Login.Commands;

public class ChangeProfile
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
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
            var entity = _db.Users
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            entity.FirstName = request.FirstName;
            entity.LastName = request.LastName;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}