using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.NavigationMenuItems.Commands;

public class CreateNavigationMenuItem
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid NavigationMenuId { get; init; }
        public required string Label { get; init; }
        public required string Url { get; init; }
        public bool IsDisabled { get; init; }
        public bool OpenInNewTab { get; init; }
        public string? CssClassName { get; init; }
        public ShortGuid? ParentNavigationMenuItemId { get; init; }

        public static Command Empty() =>
            new()
            {
                NavigationMenuId = string.Empty,
                Label = string.Empty,
                Url = string.Empty,
            };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.NavigationMenuId).NotEmpty();
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.Url).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, _) =>
                    {
                        if (!db.NavigationMenus.Any(nm => nm.Id == request.NavigationMenuId.Guid))
                            throw new NotFoundException(
                                "Navigation Menu",
                                request.NavigationMenuId
                            );

                        if (request.ParentNavigationMenuItemId.HasValue)
                            if (
                                !db.NavigationMenuItems.Any(nmi =>
                                    nmi.Id == request.ParentNavigationMenuItemId.Value.Guid
                                )
                            )
                                throw new NotFoundException(
                                    "Navigation Menu Item",
                                    request.ParentNavigationMenuItemId
                                );
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IMediator _mediator;

        public Handler(IRaythaDbContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var parentNavigationMenuItemId = request.ParentNavigationMenuItemId?.Guid;
            var navigationMenuInfo = await _db
                .NavigationMenus.Select(nm => new
                {
                    nm.Id,
                    ThisLevelNavigationMenuItemsCount = nm.NavigationMenuItems.Count(nmi =>
                        nmi.ParentNavigationMenuItemId == parentNavigationMenuItemId
                    ),
                })
                .FirstAsync(nm => nm.Id == request.NavigationMenuId.Guid, cancellationToken);

            var entity = new NavigationMenuItem
            {
                Id = Guid.NewGuid(),
                Label = request.Label,
                Url = request.Url,
                IsDisabled = request.IsDisabled,
                OpenInNewTab = request.OpenInNewTab,
                CssClassName = request.CssClassName,
                Ordinal = navigationMenuInfo.ThisLevelNavigationMenuItemsCount + 1,
                NavigationMenuId = request.NavigationMenuId,
                ParentNavigationMenuItemId = request.ParentNavigationMenuItemId,
            };

            await _mediator.Send(
                new CreateNavigationMenuRevision.Command
                {
                    NavigationMenuId = navigationMenuInfo.Id,
                },
                cancellationToken
            );

            await _db.NavigationMenuItems.AddAsync(entity, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
