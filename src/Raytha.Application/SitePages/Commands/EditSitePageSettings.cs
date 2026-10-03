using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.SitePages.Commands;

public class EditSitePageSettings
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string RoutePath { get; init; } = string.Empty;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.RoutePath).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (request.Id == ShortGuid.Empty)
                        {
                            context.AddFailure(Constants.VALIDATION_SUMMARY, "Id is required.");
                            return;
                        }

                        var entity = db.SitePages.FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("Site Page", request.Id);

                        var path = RoutePaths.Normalize(request.RoutePath);
                        var problem = RoutePaths.Problem(db, path, entity.RouteId);
                        if (problem != null)
                        {
                            context.AddFailure("RoutePath", problem);
                            return;
                        }
                        if (RoutePaths.IsTaken(db, path, entity.RouteId))
                        {
                            context.AddFailure(
                                "RoutePath",
                                $"The route path '{path}' already exists."
                            );
                            return;
                        }
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
            var entity = _db.SitePages.Include(p => p.Route).First(p => p.Id == request.Id.Guid);

            entity.Route.Path = RoutePaths.Normalize(request.RoutePath);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}

