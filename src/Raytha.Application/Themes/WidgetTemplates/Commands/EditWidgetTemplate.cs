using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WidgetTemplates.Commands;

public class EditWidgetTemplate
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public required string Label { get; init; }
        public required string Content { get; init; }

        public static Command Empty() =>
            new() { Label = string.Empty, Content = string.Empty };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty().WithMessage("Label is required.");
            RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db.WidgetTemplates.FirstOrDefault(p =>
                            p.Id == request.Id.Guid
                        );

                        if (entity == null)
                            throw new NotFoundException("Widget Template", request.Id);
                    }
                );
        }
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
            var entity = await _db
                .WidgetTemplates.FirstAsync(wt => wt.Id == request.Id.Guid, cancellationToken);

            // Create revision before updating
            var revision = new WidgetTemplateRevision
            {
                WidgetTemplateId = entity.Id,
                Content = entity.Content,
                Label = entity.Label,
            };

            _db.WidgetTemplateRevisions.Add(revision);

            entity.Label = request.Label;
            entity.Content = request.Content;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}

