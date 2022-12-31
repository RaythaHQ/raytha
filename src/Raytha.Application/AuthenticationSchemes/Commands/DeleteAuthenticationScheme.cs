using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.AuthenticationSchemes.Commands;

public class DeleteAuthenticationScheme
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.AuthenticationSchemes.FirstOrDefault(p => p.Id == request.Id.Guid);
                if (entity == null)
                    throw new NotFoundException("Authentication Scheme", request.Id);

                if (entity.IsBuiltInAuth)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "You cannot remove this built-in authentication scheme.");
                    return;
                }

                var onlyOneAdminAuthLeft = db.AuthenticationSchemes.Count(p => p.IsEnabledForAdmins) == 1;
                if (entity.IsEnabledForAdmins && onlyOneAdminAuthLeft)
                {
                    context.AddFailure("IsEnabledForAdmins", "You must have at least 1 authentication scheme enabled for administrators.");
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
            var entity = _db.AuthenticationSchemes.First(p => p.Id == request.Id.Guid);

            _db.AuthenticationSchemes.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
