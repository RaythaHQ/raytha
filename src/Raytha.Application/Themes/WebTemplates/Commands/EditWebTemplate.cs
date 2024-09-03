using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WebTemplates.Commands;

public class EditWebTemplate
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public required string Label { get; init; }
        public required string Content { get; init; }
        public bool IsBaseLayout { get; init; }
        public ShortGuid? ParentTemplateId { get; init; }
        public bool AllowAccessForNewContentTypes { get; init; }
        public required IEnumerable<ShortGuid> TemplateAccessToModelDefinitions { get; init; }

        public static Command Empty() => new()
        {
            Label = string.Empty,
            Content = string.Empty,
            TemplateAccessToModelDefinitions = new List<ShortGuid>(),
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.Content).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().Must(WebTemplateExtensions.HasRenderBodyTag).When(p => p.IsBaseLayout)
                .WithMessage("Content must have the {% renderbody %} tag if it is a base layout.");
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.WebTemplates
                    .Select(wt => new { wt.Id, wt.ThemeId })
                    .FirstOrDefault(p => p.Id == request.Id.Guid);

                if (entity == null)
                    throw new NotFoundException("Template", request.Id);

                var nonBaseLayoutsAllowedForNewTypesCount = db.WebTemplates
                    .Count(wt => !wt.IsBaseLayout && wt.AllowAccessForNewContentTypes && wt.ThemeId == entity.ThemeId);

                if (nonBaseLayoutsAllowedForNewTypesCount == 1)
                {
                    if (entity.Id == request.Id.Guid)
                    {
                        if (request.IsBaseLayout || !request.AllowAccessForNewContentTypes)
                        {
                            context.AddFailure("AllowAccessForNewContentTypes", "This is currently the only template that new content types can access. You must have at least 1 non base layout template new content types can default to.");
                            return;
                        }
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
            var entity = await _db.WebTemplates
                .Include(wt => wt.TemplateAccessToModelDefinitions)
                .FirstAsync(wt => wt.Id == request.Id.Guid, cancellationToken);

            if (request.ParentTemplateId.HasValue && request.ParentTemplateId != Guid.Empty)
            {
                var parent = await _db.WebTemplates
                    .IncludeParentTemplates(wt => wt.ParentTemplate)
                    .FirstAsync(wt => wt.ThemeId == entity.ThemeId && wt.Id == request.ParentTemplateId.Value.Guid, cancellationToken);

                var iterator = parent;
                while (iterator != null)
                {
                    if (iterator.Id == entity.Id)
                        throw new BusinessException("A circular dependency was detected with this base layout relationship.");

                    iterator = iterator.ParentTemplate;
                }
            }

            if (!request.IsBaseLayout && entity.IsBaseLayout)
            {
                var hasChildTemplates = _db.WebTemplates
                    .Any(wt => wt.ParentTemplateId == entity.Id);

                if (hasChildTemplates)
                    throw new BusinessException("This template has other templates that inherit from it.");
            }

            var revision = new WebTemplateRevision
            {
                WebTemplateId = entity.Id,
                Content = entity.Content,
                Label = entity.Label,
                AllowAccessForNewContentTypes = entity.AllowAccessForNewContentTypes
            };

            _db.WebTemplateRevisions.Add(revision);

            entity.Label = request.Label;
            entity.Content = request.Content;
            entity.ParentTemplateId = request.ParentTemplateId.HasValue && request.ParentTemplateId != Guid.Empty ? request.ParentTemplateId : null;
            if (!entity.IsBuiltInTemplate)
            {
                entity.IsBaseLayout = request.IsBaseLayout;
            }
            entity.AllowAccessForNewContentTypes = request.AllowAccessForNewContentTypes;

            var accessToRemoveItemGuids = entity.TemplateAccessToModelDefinitions.Select(p => (ShortGuid)p.Id).Except(request.TemplateAccessToModelDefinitions ?? new List<ShortGuid>());
            var accessToRemoveItems = entity.TemplateAccessToModelDefinitions?.Where(p => accessToRemoveItemGuids.Contains(p.Id));
            var accessToAddItemGuids = request.TemplateAccessToModelDefinitions?.Except(entity.TemplateAccessToModelDefinitions?.ToList().Select(p => (ShortGuid)p.Id) ?? new List<ShortGuid>());

            if (accessToRemoveItems != null)
            {
                _db.WebTemplateAccessToModelDefinitions.RemoveRange(accessToRemoveItems);
            }

            if (accessToAddItemGuids != null)
            {
                _db.WebTemplateAccessToModelDefinitions.AddRange(accessToAddItemGuids.Select(p => new WebTemplateAccessToModelDefinition
                {
                    WebTemplateId = entity.Id,
                    ContentTypeId = p
                }));
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
