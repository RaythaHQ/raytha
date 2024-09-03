using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.NavigationMenus.Commands;

namespace Raytha.Application.NavigationMenuItems.Commands;

public class EditNavigationMenuItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public required string Label { get; init; }
        public required string Url { get; init; }
        public bool IsDisabled { get; init; }
        public bool OpenInNewTab { get; init; }
        public string? CssClassName { get; init; }
        public ShortGuid? ParentNavigationMenuItemId { get; init; }
        public required ShortGuid NavigationMenuId { get; init; }

        public static Command Empty() => new()
        {
            Label = string.Empty,
            Url = string.Empty,
            NavigationMenuId = string.Empty,
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.Url).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (!db.NavigationMenuItems.Any(nmi => nmi.Id == request.Id.Guid))
                {
                    throw new NotFoundException("Navigation Menu Item", request.Id);
                }

                if (request.ParentNavigationMenuItemId.HasValue)
                {
                    if (!db.NavigationMenuItems.Any(nmi => nmi.Id == request.ParentNavigationMenuItemId.Value.Guid))
                    {
                        throw new NotFoundException("Navigation Menu Item", request.ParentNavigationMenuItemId);
                    }

                    var nestedNavigationMenuItemIds = db.NavigationMenuItems
                        .Where(nmi => nmi.NavigationMenuId == request.NavigationMenuId.Guid)
                        .Select(NavigationMenuItemDto.GetProjection())
                        .ToArray()
                        .GetNestedNavigationMenuItemIds(request.Id);

                    if (nestedNavigationMenuItemIds.Any(id => id == request.ParentNavigationMenuItemId.Value) || request.Id == request.ParentNavigationMenuItemId)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, "Cannot select a nested navigation menu item as the parent navigation menu item.");
                    }
                }
            });
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

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entity = await _db.NavigationMenuItems
                .FirstAsync(nmi => nmi.Id == request.Id.Guid, cancellationToken);

            entity.Url = request.Url;
            entity.Label = request.Label;
            entity.IsDisabled = request.IsDisabled;
            entity.OpenInNewTab = request.OpenInNewTab;
            entity.CssClassName = request.CssClassName;
            entity.ParentNavigationMenuItemId = request.ParentNavigationMenuItemId;

            await _mediator.Send(new CreateNavigationMenuRevision.Command
            {
                NavigationMenuId = entity.NavigationMenuId,
            }, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}