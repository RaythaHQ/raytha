using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.MediaItems.Commands;

public class CreateMediaItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public long Length { get; init; }
        public string FileName { get; init; } = null!;
        public string ContentType { get; init; } = null!;
        public string FileStorageProvider { get; init; } = null;
        public string ObjectKey { get; init; } 
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

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
