using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.RaythaFunctions.Commands;

public class CreateRaythaFunction
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required string Name { get; init; }
        public required string DeveloperName { get; init; }
        public required string TriggerType { get; init; }
        public bool IsActive { get; init; }
        public required string Code { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.TriggerType).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (db.RaythaFunctions.Any(p => p.DeveloperName == request.DeveloperName.ToDeveloperName()))
                    context.AddFailure("DeveloperName", $"A function with the developer name {request.DeveloperName.ToDeveloperName()} already exists.");
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
            var function = new RaythaFunction
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                TriggerType = RaythaFunctionTriggerType.From(request.TriggerType),
                IsActive = request.IsActive,
                Code = request.Code,
            };

            await _db.RaythaFunctions.AddAsync(function, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(function.Id);
        }
    }
}
