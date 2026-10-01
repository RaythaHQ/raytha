using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WidgetTemplates.Commands;

public class CreateWidgetTemplate
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid ThemeId { get; init; }
        public string Label { get; init; } = string.Empty;
        public string DeveloperName { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;

        /// <summary>The settings form in display order.</summary>
        public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, ILiquidTemplateParser liquid)
        {
            RuleFor(x => x.ThemeId).NotEmpty();
            RuleFor(x => x.Label).NotEmpty().WithMessage("Label is required.");
            RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");
            RuleFor(x => x.Content)
                .Custom((content, context) => LiquidSyntaxValidation.RejectInvalidLiquid(context, liquid, content));
            RuleFor(x => x.DeveloperName)
                .Must(name => !string.IsNullOrEmpty(name.ToDeveloperName()))
                .WithMessage("Developer name is required.");
            RuleFor(x => x.DeveloperName)
                .Must(name => !BuiltInWidgetType.IsBuiltIn(name.ToDeveloperName()))
                .WithMessage("That developer name is reserved for a built-in widget.");
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (!db.Themes.Any(t => t.Id == request.ThemeId.Guid))
                            throw new NotFoundException("Theme", request.ThemeId);

                        var developerName = request.DeveloperName.ToDeveloperName();
                        if (
                            db.WidgetTemplates.Any(wt =>
                                wt.ThemeId == request.ThemeId.Guid && wt.DeveloperName == developerName
                            )
                        )
                        {
                            context.AddFailure(
                                "DeveloperName",
                                "A widget template with that developer name already exists in this theme."
                            );
                        }

                        foreach (var error in WidgetFieldDefinitions.Validate(request.Fields ?? []))
                            context.AddFailure("Fields", error);
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
            var entity = new WidgetTemplate
            {
                Id = Guid.NewGuid(),
                ThemeId = request.ThemeId.Guid,
                Label = request.Label,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                Content = request.Content,
                IsBuiltInTemplate = false,
                Fields = request.Fields ?? [],
            };

            _db.WidgetTemplates.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
