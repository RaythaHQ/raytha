using CSharpVitamins;
using FluentValidation;
using Mediator;
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

        /// <summary>Optional public path such as llms.txt. HTTP request functions only.</summary>
        public string? RoutePath { get; init; }

        public static Command Empty() =>
            new()
            {
                Name = string.Empty,
                DeveloperName = string.Empty,
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
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var developerName = request.DeveloperName.ToDeveloperName(allowDot: true);
                        if (db.RaythaFunctions.Any(p => p.DeveloperName == developerName))
                            context.AddFailure(
                                "DeveloperName",
                                $"A function with the developer name {developerName} already exists."
                            );

                        var routePath = RaythaFunctionRoutePath.Normalize(request.RoutePath);
                        if (routePath.Length == 0)
                            return;
                        var routeProblem = RaythaFunctionRoutePath.Problem(
                            db,
                            routePath,
                            request.TriggerType,
                            ownRouteId: null
                        );
                        if (routeProblem != null)
                            context.AddFailure("RoutePath", routeProblem);
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

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var function = new RaythaFunction
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DeveloperName = request.DeveloperName.ToDeveloperName(allowDot: true),
                TriggerType = RaythaFunctionTriggerType.From(request.TriggerType),
                IsActive = request.IsActive,
                Code = request.Code,
            };
            RaythaFunctionRoutePath.Apply(
                _db,
                function,
                RaythaFunctionRoutePath.Normalize(request.RoutePath)
            );

            await _db.RaythaFunctions.AddAsync(function, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(function.Id);
        }
    }
}
