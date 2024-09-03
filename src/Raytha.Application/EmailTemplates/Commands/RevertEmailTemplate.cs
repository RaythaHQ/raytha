using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.EmailTemplates.Commands;

public class RevertEmailTemplate
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
            var entity = _db.EmailTemplateRevisions
                    .Include(p => p.EmailTemplate)
                    .First(p => p.Id == request.Id.Guid);

            var newRevision = new EmailTemplateRevision
            {
                EmailTemplateId = entity.EmailTemplateId,
                Content = entity.EmailTemplate.Content,
                Subject = entity.EmailTemplate.Subject,
                Cc = entity.EmailTemplate.Cc,
                Bcc = entity.EmailTemplate.Bcc,
            };

            _db.EmailTemplateRevisions.Add(newRevision);
            entity.EmailTemplate.Subject = entity.Subject;
            entity.EmailTemplate.Content = entity.Content;
            entity.EmailTemplate.Bcc = entity.Bcc;
            entity.EmailTemplate.Cc = entity.Cc;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.EmailTemplateId);
        }
    }
}
