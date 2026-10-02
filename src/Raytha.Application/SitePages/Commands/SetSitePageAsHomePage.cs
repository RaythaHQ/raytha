using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages.Commands;

public class SetSitePageAsHomePage
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (
                            !HomePageValidation.IsPublished(
                                db,
                                request.Id.Guid,
                                Route.SITE_PAGE_TYPE
                            )
                        )
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                HomePageValidation.CannotSetUnpublishedMessage
                            );
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
            var entity = _db.OrganizationSettings.First();

            var sitePage = _db.SitePages.FirstOrDefault(p => p.Id == request.Id.Guid);
            if (sitePage == null)
                throw new NotFoundException("Site page not found", request.Id);

            entity.HomePageId = request.Id.Guid;
            entity.HomePageType = Route.SITE_PAGE_TYPE;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}

