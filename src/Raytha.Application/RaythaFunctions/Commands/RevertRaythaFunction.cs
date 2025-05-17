using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.RaythaFunctions.Commands;

public class RevertRaythaFunction
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, _) =>
                    {
                        if (!db.RaythaFunctionRevisions.Any(p => p.Id == request.Id.Guid))
                            throw new NotFoundException("Raytha Function Revision", request.Id);
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

        public async Task<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var revision = await _db
                .RaythaFunctionRevisions.Include(rfr => rfr.RaythaFunction)
                .FirstAsync(rfr => rfr.Id == request.Id.Guid, cancellationToken);

            var function = revision.RaythaFunction!;
            await _db.RaythaFunctionRevisions.AddAsync(
                new RaythaFunctionRevision { RaythaFunctionId = function.Id, Code = function.Code },
                cancellationToken
            );

            function.Code = revision.Code;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(revision.RaythaFunctionId);
        }
    }
}
