using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.NavigationMenuItems;
using Raytha.Domain.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Raytha.Application.NavigationMenus.Commands;

public class CreateNavigationMenuRevision
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid NavigationMenuId { get; init; }

        public static Command Empty() => new()
        {
            NavigationMenuId = string.Empty,
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, _) =>
            {
                if (!db.NavigationMenus.Any(nm => nm.Id == request.NavigationMenuId.Guid))
                    throw new NotFoundException("Navigation Menu", request.NavigationMenuId);
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
            var navigationMenuItems = await _db.NavigationMenuItems
                .Where(nmi => nmi.NavigationMenuId == request.NavigationMenuId.Guid)
                .Select(NavigationMenuItemJson.GetProjection())
                .ToArrayAsync(cancellationToken);

            var entity = new NavigationMenuRevision
            {
                NavigationMenuId = request.NavigationMenuId,
                NavigationMenuItemsJson = JsonSerializer.Serialize(navigationMenuItems),
            };

            await _db.NavigationMenuRevisions.AddAsync(entity, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}