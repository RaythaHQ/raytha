using CSharpVitamins;
using FluentValidation;
using Mediator;
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

        /// <summary>Optional public path such as llms.txt. Empty removes it; HTTP request functions only.</summary>
        public string? RoutePath { get; init; }

        public static Command Empty() =>
            new()
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
            RuleFor(x => x.TriggerType)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(type =>
                    RaythaFunctionTriggerType.SupportedTypes.Any(t => t.DeveloperName == type)
                )
                .WithMessage(
                    "Trigger type must be one of: "
                        + string.Join(
                            ", ",
                            RaythaFunctionTriggerType.SupportedTypes.Select(t => t.DeveloperName)
                        )
                        + "."
                );
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var function = db
                            .RaythaFunctions.Where(p => p.Id == request.Id.Guid)
                            .Select(p => new { p.RouteId })
                            .FirstOrDefault();
                        if (function == null)
                            throw new NotFoundException("Raytha Function", request.Id);

                        var routePath = RaythaFunctionRoutePath.Normalize(request.RoutePath);
                        if (routePath.Length == 0)
                            return;
                        var routeProblem = RaythaFunctionRoutePath.Problem(
                            db,
                            routePath,
                            request.TriggerType,
                            function.RouteId
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
            var function = await _db
                .RaythaFunctions.Include(rf => rf.Route)
                .FirstAsync(rf => rf.Id == request.Id.Guid, cancellationToken);

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
            RaythaFunctionRoutePath.Apply(
                _db,
                function,
                function.TriggerType.Equals(RaythaFunctionTriggerType.HttpRequest)
                    ? RaythaFunctionRoutePath.Normalize(request.RoutePath)
                    : string.Empty
            );

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
