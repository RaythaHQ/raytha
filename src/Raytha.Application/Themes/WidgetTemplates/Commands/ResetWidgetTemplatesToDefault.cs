using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WidgetTemplates.Commands;

public class ResetWidgetTemplatesToDefault
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required string ThemeId { get; init; }
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
            ShortGuid themeId = request.ThemeId;
            var theme = await _db.Themes.FirstOrDefaultAsync(
                t => t.Id == themeId.Guid,
                cancellationToken
            );

            if (theme == null)
            {
                return new CommandResponseDto<ShortGuid>("ThemeId", "Theme not found.");
            }

            var existingTemplates = await _db
                .WidgetTemplates.Where(w => w.ThemeId == themeId.Guid)
                .ToListAsync(cancellationToken);

            var existingByDeveloperName = existingTemplates.ToDictionary(
                t => t.DeveloperName ?? string.Empty,
                t => t
            );

            foreach (var widgetType in BuiltInWidgetType.WidgetTypes)
            {
                if (existingByDeveloperName.TryGetValue(widgetType.DeveloperName, out var existing))
                {
                    _db.WidgetTemplateRevisions.Add(existing.ToRevision());
                    widgetType.ResetTemplate(existing);
                }
                else
                {
                    _db.WidgetTemplates.Add(widgetType.CreateTemplate(themeId.Guid));
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(themeId);
        }
    }
}
