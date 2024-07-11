using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.Commands;

public class DeleteTheme
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                var activeThemeId = db.OrganizationSettings
                    .Select(os => os.ActiveThemeId)
                    .First();

                if (request.Id.Guid == activeThemeId)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "You cannot delete an active theme. Set another theme as the active theme before deleting this one.");

                    return;
                }

                var theme = db.Themes
                    .Where(t => t.Id == request.Id.Guid)
                    .Select(t => new
                    {
                        t.DeveloperName,
                    }).FirstOrDefault();

                if (theme == null)
                {
                    throw new NotFoundException("Theme", request.Id);
                }
                
                if (theme.DeveloperName == Theme.DEFAULT_THEME_DEVELOPER_NAME)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "You cannot delete the default theme.");

                    return;
                }
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IFileStorageProvider _fileStorageProvider;

        public Handler(IRaythaDbContext db, IFileStorageProvider fileStorageProvider)
        {
            _db = db;
            _fileStorageProvider = fileStorageProvider;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var theme = await _db.Themes
                .Include(t => t.ThemeAccessToMediaItems)
                    .ThenInclude(mi => mi.MediaItem)
                .FirstAsync(t => t.Id == request.Id.Guid, cancellationToken);

            foreach (var themeAccessToMediaItem in theme.ThemeAccessToMediaItems)
            {
                await _fileStorageProvider.DeleteAsync(themeAccessToMediaItem.MediaItem!.ObjectKey);
            }

            _db.Themes.Remove(theme);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(theme.Id);
        }
    }
}