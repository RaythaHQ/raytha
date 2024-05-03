using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.RaythaFunctions.Commands;

public class EditRaythaFunction
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public required string Name { get; init; }
        public required string TriggerType { get; init; }
        public bool IsActive { get; init; }
        public required string Code { get; init; }

        public static Command Empty() => new()
        {
            Name = string.Empty,
            TriggerType = string.Empty,
            Code = string.Empty,
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.TriggerType).NotEmpty();
            RuleFor(x => x).Custom((request, _) =>
            {
                if (!db.RaythaFunctions.Any(p => p.Id == request.Id.Guid))
                    throw new NotFoundException("Raytha Function", request.Id);
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
            var function = await _db.RaythaFunctions.FirstAsync(rf => rf.Id == request.Id.Guid, cancellationToken);

            if (!function.Code.Equals(request.Code))
            {
                var revision = new RaythaFunctionRevision
                {
                    RaythaFunctionId = function.Id,
                    Code = function.Code,
                };

                await _db.RaythaFunctionRevisions.AddAsync(revision, cancellationToken);
            }

            function.Name = request.Name;
            function.TriggerType = RaythaFunctionTriggerType.From(request.TriggerType);
            function.IsActive = request.IsActive;
            function.Code = request.Code;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
