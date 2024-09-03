using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.EmailTemplates.Commands;

public class EditEmailTemplate
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string Subject { get; init; } = null!;
        public string Content { get; init; } = null!;
        public string Bcc { get; init; } = string.Empty;
        public string Cc { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Subject).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.EmailTemplates.FirstOrDefault(p => p.Id == request.Id.Guid);
                if (entity == null)
                    throw new NotFoundException("EmailTemplate", request.Id);

                var ccArray = request.Cc.SplitIntoSeparateEmailAddresses();
                var bccArray = request.Bcc.SplitIntoSeparateEmailAddresses();

                if (ccArray.Any())
                {
                    foreach (var cc in ccArray)
                    {
                        context.AddFailure("Cc", $"{cc} is not in a valid email format.");
                        return;
                    }
                }

                if (bccArray.Any())
                {
                    foreach (var bcc in bccArray)
                    {
                        context.AddFailure("Bcc", $"{bcc} is not in a valid email format.");
                        return;
                    }
                }
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
            var entity = _db.EmailTemplates
                    .First(p => p.Id == request.Id.Guid);

            if (BuiltInEmailTemplate.Templates.Any(p => p.DeveloperName == entity.DeveloperName) && !BuiltInEmailTemplate.From(entity.DeveloperName).SafeToCc)
            {
                if (!string.IsNullOrEmpty(request.Cc) || !string.IsNullOrEmpty(request.Bcc))
                {
                    throw new InvalidOperationException($"Cannot set CC or BCC on {entity.DeveloperName} for security reasons.");
                }
            }

            var revision = new EmailTemplateRevision
            {
                EmailTemplateId = entity.Id,
                Content = entity.Content,
                Subject = entity.Subject,
                Cc = entity.Cc,
                Bcc = entity.Bcc
            };

            _db.EmailTemplateRevisions.Add(revision);

            entity.Subject = request.Subject;
            entity.Content = request.Content;
            entity.Cc = request.Cc;
            entity.Bcc = request.Bcc;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
