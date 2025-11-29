using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WidgetTemplates.Commands;

public class RevertWidgetTemplate
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

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
            var entity = _db
                .WidgetTemplateRevisions.Include(p => p.WidgetTemplate)
                .First(p => p.Id == request.Id.Guid);

            var newRevision = new WidgetTemplateRevision
            {
                WidgetTemplateId = entity.WidgetTemplateId,
                Content = entity.WidgetTemplate!.Content,
                Label = entity.WidgetTemplate.Label,
            };

            _db.WidgetTemplateRevisions.Add(newRevision);

            entity.WidgetTemplate.Label = entity.Label;
            entity.WidgetTemplate.Content = entity.Content;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.WidgetTemplateId);
        }
    }
}

