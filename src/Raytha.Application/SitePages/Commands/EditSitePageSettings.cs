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

                        var slugifiedPath = request.RoutePath.ToUrlSlug();
                        if (string.IsNullOrWhiteSpace(slugifiedPath))
                        {
                            context.AddFailure(
                                "RoutePath",
                                "Invalid route path. Must be letters, numbers, dashes, and dots (dots only allowed in the last segment for file extensions like .txt or .xml)"
                            );
                            return;
                        }
                        if (!slugifiedPath.IsValidRoutePath())
                        {
                            context.AddFailure(
                                "RoutePath",
                                "Invalid route path. Dots are only allowed in the last segment (e.g., robots.txt). No '..' segments or leading dots allowed."
                            );
                            return;
                        }
                        if (slugifiedPath.Length > 200)
                        {
                            context.AddFailure(
                                "RoutePath",
                                "Invalid route path. Must be less than 200 characters"
                            );
                            return;
                        }
                        
                        // Get the current SitePage's route ID to exclude it from the check
                        var currentRouteId = entity.RouteId;
                        
                        // Check if any OTHER route has this path (case-insensitive)
                        var routePathExists = db.Routes.Any(p =>
                            p.Path.ToLower() == slugifiedPath.ToLower() && p.Id != currentRouteId
                        );
                        if (routePathExists)
                        {
                            context.AddFailure(
                                "RoutePath",
                                $"The route path '{slugifiedPath}' already exists."
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

            entity.Route.Path = request.RoutePath.ToUrlSlug();

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}

