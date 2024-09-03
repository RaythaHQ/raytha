using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WebTemplates.Commands;

public class CreateWebTemplate
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid ThemeId { get; init; }
        public required string DeveloperName { get; init; }
        public required string Label { get; init; }
        public required string Content { get; init; }
        public bool IsBaseLayout { get; init; }
        public ShortGuid? ParentTemplateId { get; init; }
        public bool AllowAccessForNewContentTypes { get; init; }
        public required IEnumerable<ShortGuid> TemplateAccessToModelDefinitions { get; init; }

        public static Command Empty() => new()
        {
            ThemeId = ShortGuid.Empty,
            Label = string.Empty,
            DeveloperName = string.Empty,
            Content = string.Empty,
            TemplateAccessToModelDefinitions = new List<ShortGuid>(),
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.ThemeId).NotEmpty();
            RuleFor(x => x).Custom((request, _) =>
            {
                if (!db.Themes.Any(t => t.Id == request.ThemeId.Guid))
                    throw new NotFoundException("Theme", request.ThemeId);
            });
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.DeveloperName).Must(StringExtensions.IsValidDeveloperName).WithMessage("Invalid developer name.");
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().Must(WebTemplateExtensions.HasRenderBodyTag).When(p => p.IsBaseLayout)
                .WithMessage("Content must have the {% renderbody %} tag if it is a base layout.");
            RuleFor(x => x).Custom((request, context) =>
            {
                if (db.WebTemplates.Any(wt => wt.ThemeId == request.ThemeId.Guid && wt.DeveloperName == request.DeveloperName.ToDeveloperName()))
                    context.AddFailure("A template with that developer name already exists.");
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
            var webTemplate = new WebTemplate
            {
                Id = Guid.NewGuid(),
                ThemeId = request.ThemeId.Guid,
                Label = request.Label,
                Content = request.Content,
                ParentTemplateId = request.ParentTemplateId.HasValue && request.ParentTemplateId != Guid.Empty ? request.ParentTemplateId : null,
                IsBaseLayout = request.IsBaseLayout,
                IsBuiltInTemplate = false,
                AllowAccessForNewContentTypes = request.AllowAccessForNewContentTypes,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                TemplateAccessToModelDefinitions = request.TemplateAccessToModelDefinitions?.Select(p => new WebTemplateAccessToModelDefinition
                {
                    ContentTypeId = p,
                }).ToList(),
            };

            await _db.WebTemplates.AddAsync(webTemplate, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(webTemplate.Id);
        }
    }
}