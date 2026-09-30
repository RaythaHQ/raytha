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

namespace Raytha.Application.Themes.WidgetTemplates.Commands;

public class EditWidgetTemplateByDeveloperName
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

        /// <summary>The settings form in display order. Null leaves the fields unchanged.</summary>
        public IReadOnlyList<FieldDefinition>? Fields { get; init; }

        public static Command Empty() => new() { Label = string.Empty, Content = string.Empty };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.ThemeDeveloperName).NotEmpty();
            RuleFor(x => x.TemplateDeveloperName).NotEmpty();
            RuleFor(x => x.Label).NotEmpty().WithMessage("Label is required.");
            RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");
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
                            .WidgetTemplates.Where(wt =>
                                wt.ThemeId == theme.Id
                                && wt.DeveloperName
                                    == request.TemplateDeveloperName.ToDeveloperName()
                            )
                            .Select(wt => new { wt.Id })
                            .FirstOrDefault();

                        if (entity == null)
                            throw new NotFoundException(
                                "Widget Template",
                                request.TemplateDeveloperName
                            );

                        if (request.Fields != null)
                        {
                            foreach (var error in WidgetFieldDefinitions.Validate(request.Fields))
                                context.AddFailure("Fields", error);
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
                .WidgetTemplates.FirstAsync(
                    wt =>
                        wt.ThemeId == theme.Id
                        && wt.DeveloperName == request.TemplateDeveloperName.ToDeveloperName(),
                    cancellationToken
                );

            _db.WidgetTemplateRevisions.Add(entity.ToRevision());

            entity.Label = request.Label;
            entity.Content = request.Content;
            if (request.Fields != null)
            {
                entity.Fields = request.Fields;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}

