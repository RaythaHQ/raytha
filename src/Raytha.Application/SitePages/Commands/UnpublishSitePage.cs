using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.Webhooks;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages.Commands;

[WebhookEvent("site_page.unpublished", DisplayName = "Site page unpublished", Group = "Site pages")]
public class UnpublishSitePage
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
                            HomePageValidation.IsHomePage(
                                db,
                                request.Id.Guid,
                                Route.SITE_PAGE_TYPE
                            )
                        )
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                HomePageValidation.CannotUnpublishMessage
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
            var entity = await _db.SitePages.FirstOrDefaultAsync(
                p => p.Id == request.Id.Guid,
                cancellationToken
            );

            if (entity == null)
                throw new NotFoundException("Site Page", request.Id);

            entity.IsPublished = false;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}

