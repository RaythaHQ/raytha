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

public class EditWebTemplateByDeveloperName
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        /// <summary>
        /// Set from route parameter, not from request body.
        /// </summary>
        [JsonIgnore]
        public string ThemeDeveloperName { get; init; } = string.Empty;

        /// <summary>
        /// Set from route parameter, not from request body.
        /// </summary>
        [JsonIgnore]
        public string TemplateDeveloperName { get; init; } = string.Empty;
        public required string Label { get; init; }
        public required string Content { get; init; }
        public bool IsBaseLayout { get; init; }
        public string? ParentTemplateDeveloperName { get; init; }
        public bool AllowAccessForNewContentTypes { get; init; }
        public IEnumerable<string>? TemplateAccessToModelDefinitions { get; init; }

        public static Command Empty() => new() { Label = string.Empty, Content = string.Empty };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.ThemeDeveloperName).NotEmpty();
            RuleFor(x => x.TemplateDeveloperName).NotEmpty();
            RuleFor(x => x.Label).NotEmpty();
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

                        var entity = db
                            .WebTemplates.Where(wt =>
                                wt.ThemeId == theme.Id
                                && wt.DeveloperName
                                    == request.TemplateDeveloperName.ToDeveloperName()
                            )
                            .Select(wt => new { wt.Id, wt.ThemeId })
                            .FirstOrDefault();

                        if (entity == null)
                            throw new NotFoundException("Template", request.TemplateDeveloperName);

                        var nonBaseLayoutsAllowedForNewTypesCount = db.WebTemplates.Count(wt =>
                            !wt.IsBaseLayout
                            && wt.AllowAccessForNewContentTypes
                            && wt.ThemeId == entity.ThemeId
                        );

                        if (nonBaseLayoutsAllowedForNewTypesCount == 1)
                        {
                            var currentIsTheOnlyOne = db
                                .WebTemplates.Where(wt =>
                                    !wt.IsBaseLayout
                                    && wt.AllowAccessForNewContentTypes
                                    && wt.ThemeId == entity.ThemeId
                                )
                                .Select(wt => wt.Id)
                                .FirstOrDefault();

                            if (currentIsTheOnlyOne == entity.Id)
                            {
                                if (request.IsBaseLayout || !request.AllowAccessForNewContentTypes)
                                {
                                    context.AddFailure(
                                        "AllowAccessForNewContentTypes",
                                        "This is currently the only template that new content types can access. You must have at least 1 non base layout template new content types can default to."
                                    );
                                    return;
                                }
                            }
                        }
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

            var entity = await _db
                .WebTemplates.Include(wt => wt.TemplateAccessToModelDefinitions)
                .FirstAsync(
                    wt =>
                        wt.ThemeId == theme.Id
                        && wt.DeveloperName == request.TemplateDeveloperName.ToDeveloperName(),
                    cancellationToken
                );

            Guid? parentTemplateId = null;
            if (!string.IsNullOrEmpty(request.ParentTemplateDeveloperName))
            {
                var parentTemplate = await _db
                    .WebTemplates.IncludeParentTemplates(wt => wt.ParentTemplate)
                    .FirstOrDefaultAsync(
                        wt =>
                            wt.ThemeId == theme.Id
                            && wt.DeveloperName
                                == request.ParentTemplateDeveloperName.ToDeveloperName(),
                        cancellationToken
                    );

                if (parentTemplate == null)
                    throw new NotFoundException(
                        "Parent Template",
                        request.ParentTemplateDeveloperName
                    );

                var iterator = parentTemplate;
                while (iterator != null)
                {
                    if (iterator.Id == entity.Id)
                        throw new BusinessException(
                            "A circular dependency was detected with this base layout relationship."
                        );

                    iterator = iterator.ParentTemplate;
                }

                parentTemplateId = parentTemplate.Id;
            }

            if (!request.IsBaseLayout && entity.IsBaseLayout)
            {
                var hasChildTemplates = _db.WebTemplates.Any(wt =>
                    wt.ParentTemplateId == entity.Id
                );

                if (hasChildTemplates)
                    throw new BusinessException(
                        "This template has other templates that inherit from it."
                    );
            }

            var revision = new WebTemplateRevision
            {
                WebTemplateId = entity.Id,
                Content = entity.Content,
                Label = entity.Label,
                AllowAccessForNewContentTypes = entity.AllowAccessForNewContentTypes,
            };

            _db.WebTemplateRevisions.Add(revision);

            entity.Label = request.Label;
            entity.Content = request.Content;
            entity.ParentTemplateId = parentTemplateId;
            if (!entity.IsBuiltInTemplate)
            {
                entity.IsBaseLayout = request.IsBaseLayout;
            }
            entity.AllowAccessForNewContentTypes = request.AllowAccessForNewContentTypes;

            // Handle template access to model definitions
            var newContentTypeIds = new List<Guid>();
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
                        newContentTypeIds.Add(contentType.Id);
                }
            }

            var existingContentTypeIds =
                entity.TemplateAccessToModelDefinitions?.Select(p => p.ContentTypeId).ToList()
                ?? new List<Guid>();

            var accessToRemoveItems = entity.TemplateAccessToModelDefinitions?.Where(p =>
                !newContentTypeIds.Contains(p.ContentTypeId)
            );

            var accessToAddItems = newContentTypeIds.Where(id =>
                !existingContentTypeIds.Contains(id)
            );

            if (accessToRemoveItems != null)
            {
                _db.WebTemplateAccessToModelDefinitions.RemoveRange(accessToRemoveItems);
            }

            _db.WebTemplateAccessToModelDefinitions.AddRange(
                accessToAddItems.Select(id => new WebTemplateAccessToModelDefinition
                {
                    WebTemplateId = entity.Id,
                    ContentTypeId = id,
                })
            );

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
