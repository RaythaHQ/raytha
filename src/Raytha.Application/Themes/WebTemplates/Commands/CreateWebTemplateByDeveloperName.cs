using System.Text.Json.Serialization;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WebTemplates.Commands;

public class CreateWebTemplateByDeveloperName
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        /// <summary>
        /// Set from route parameter, not from request body.
        /// </summary>
        [JsonIgnore]
        public string ThemeDeveloperName { get; init; } = string.Empty;
        public required string DeveloperName { get; init; }
        public required string Label { get; init; }
        public required string Content { get; init; }
        public bool IsBaseLayout { get; init; }
        public string? ParentTemplateDeveloperName { get; init; }
        public bool AllowAccessForNewContentTypes { get; init; }
        public IEnumerable<string>? TemplateAccessToModelDefinitions { get; init; }

        public static Command Empty() =>
            new()
            {
                Label = string.Empty,
                DeveloperName = string.Empty,
                Content = string.Empty,
            };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.ThemeDeveloperName).NotEmpty();
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.DeveloperName)
                .NotEmpty()
                .Must(StringExtensions.IsValidDeveloperName)
                .WithMessage("Invalid developer name.");
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.Content)
                .NotEmpty()
                .Must(WebTemplateExtensions.HasRenderBodyTag)
                .When(p => p.IsBaseLayout)
                .WithMessage("Content must have the {% renderbody %} tag if it is a base layout.");
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var theme = db
                            .Themes.Where(t =>
                                t.DeveloperName == request.ThemeDeveloperName.ToDeveloperName()
                            )
                            .Select(t => new { t.Id })
                            .FirstOrDefault();

                        if (theme == null)
                            throw new NotFoundException("Theme", request.ThemeDeveloperName);

                        if (
                            db.WebTemplates.Any(wt =>
                                wt.ThemeId == theme.Id
                                && wt.DeveloperName == request.DeveloperName.ToDeveloperName()
                            )
                        )
                            context.AddFailure(
                                "A template with that developer name already exists."
                            );
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
            var theme = await _db
                .Themes.Where(t => t.DeveloperName == request.ThemeDeveloperName.ToDeveloperName())
                .Select(t => new { t.Id })
                .FirstAsync(cancellationToken);

            Guid? parentTemplateId = null;
            if (!string.IsNullOrEmpty(request.ParentTemplateDeveloperName))
            {
                var parentTemplate = await _db
                    .WebTemplates.Where(wt =>
                        wt.ThemeId == theme.Id
                        && wt.DeveloperName == request.ParentTemplateDeveloperName.ToDeveloperName()
                    )
                    .Select(wt => new { wt.Id })
                    .FirstOrDefaultAsync(cancellationToken);

                if (parentTemplate == null)
                    throw new NotFoundException(
                        "Parent Template",
                        request.ParentTemplateDeveloperName
                    );

                parentTemplateId = parentTemplate.Id;
            }

            var contentTypeIds = new List<Guid>();
            if (request.TemplateAccessToModelDefinitions != null)
            {
                foreach (var developerName in request.TemplateAccessToModelDefinitions)
                {
                    var contentType = await _db
                        .ContentTypes.Where(ct =>
                            ct.DeveloperName == developerName.ToDeveloperName()
                        )
                        .Select(ct => new { ct.Id })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (contentType != null)
                        contentTypeIds.Add(contentType.Id);
                }
            }

            var webTemplate = new WebTemplate
            {
                Id = Guid.NewGuid(),
                ThemeId = theme.Id,
                Label = request.Label,
                Content = request.Content,
                ParentTemplateId = parentTemplateId,
                IsBaseLayout = request.IsBaseLayout,
                IsBuiltInTemplate = false,
                AllowAccessForNewContentTypes = request.AllowAccessForNewContentTypes,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                TemplateAccessToModelDefinitions = contentTypeIds
                    .Select(id => new WebTemplateAccessToModelDefinition { ContentTypeId = id })
                    .ToList(),
            };

            await _db.WebTemplates.AddAsync(webTemplate, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(webTemplate.Id);
        }
    }
}
