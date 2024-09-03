using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WebTemplates.Commands;

public class RevertWebTemplate
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
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
            var entity = _db.WebTemplateRevisions
                    .Include(p => p.WebTemplate)
                    .First(p => p.Id == request.Id.Guid);

            var newRevision = new WebTemplateRevision
            {
                WebTemplateId = entity.WebTemplateId,
                Content = entity.WebTemplate!.Content,
                Label = entity.WebTemplate.Label,
                AllowAccessForNewContentTypes = entity.WebTemplate.AllowAccessForNewContentTypes
            };

            _db.WebTemplateRevisions.Add(newRevision);

            entity.WebTemplate.Label = entity.Label;
            entity.WebTemplate.Content = entity.Content;
            entity.WebTemplate.AllowAccessForNewContentTypes = entity.AllowAccessForNewContentTypes;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.WebTemplateId);
        }
    }
}
