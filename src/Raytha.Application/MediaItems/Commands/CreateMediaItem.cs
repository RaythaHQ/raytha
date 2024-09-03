using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.MediaItems.Commands;

public class CreateMediaItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public required long Length { get; init; }
        public required string FileName { get; init; }
        public required string ContentType { get; init; }
        public required string FileStorageProvider { get; init; }
        public required string ObjectKey { get; init; }
        public ShortGuid? ThemeId { get; init; }

        public static Command Empty() => new()
        {
            Length = long.MaxValue,
            FileName = string.Empty,
            ContentType = string.Empty,
            FileStorageProvider = string.Empty,
            ObjectKey = string.Empty,

        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Length).GreaterThan(0);
            RuleFor(x => x.FileName).NotEmpty();
            RuleFor(x => x.ContentType).NotEmpty();
            RuleFor(x => x.FileStorageProvider).NotEmpty();
            RuleFor(x => x.ObjectKey).NotEmpty();
            RuleFor(x => x).Custom((request, _) =>
            {
                if (request.ThemeId.HasValue && request.ThemeId.Value != ShortGuid.Empty)
                    if (!db.Themes.Any(t => t.Id == request.ThemeId.Value.Guid))
                        throw new NotFoundException("Theme", request.ThemeId.Value.Guid);
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
            MediaItem entity = new MediaItem
            {
                Id = request.Id.Guid,
                Length = request.Length,
                FileName = request.FileName,
                ContentType = request.ContentType,
                FileStorageProvider = request.FileStorageProvider,
                ObjectKey = request.ObjectKey
            };

            _db.MediaItems.Add(entity);

            if (request.ThemeId.HasValue && request.ThemeId.Value != ShortGuid.Empty)
            {
                var themeAccessToMediaItem = new ThemeAccessToMediaItem
                {
                    Id = Guid.NewGuid(),
                    MediaItemId = entity.Id,
                    ThemeId = request.ThemeId.Value.Guid,
                };

                await _db.ThemeAccessToMediaItems.AddAsync(themeAccessToMediaItem, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
