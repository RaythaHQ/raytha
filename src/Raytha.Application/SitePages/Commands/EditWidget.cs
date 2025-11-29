using System.Text.Json;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.SitePages.Widgets;

namespace Raytha.Application.SitePages.Commands;

public class EditWidget
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        /// <summary>
        /// The site page ID.
        /// </summary>
        public ShortGuid SitePageId { get; init; }

        /// <summary>
        /// The section name containing the widget.
        /// </summary>
        public string SectionName { get; init; } = string.Empty;

        /// <summary>
        /// The widget instance ID.
        /// </summary>
        public ShortGuid WidgetId { get; init; }

        /// <summary>
        /// Updated JSON settings for the widget.
        /// </summary>
        public string SettingsJson { get; init; } = "{}";
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.SitePageId).NotEmpty().WithMessage("Site page ID is required.");
            RuleFor(x => x.SectionName).NotEmpty().WithMessage("Section name is required.");
            RuleFor(x => x.WidgetId).NotEmpty().WithMessage("Widget ID is required.");

            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db.SitePages.FirstOrDefault(p =>
                            p.Id == request.SitePageId.Guid
                        );

                        if (entity == null)
                            throw new NotFoundException("Site Page", request.SitePageId);

                        // Check section exists
                        if (!entity.Widgets.ContainsKey(request.SectionName))
                        {
                            context.AddFailure(
                                "SectionName",
                                $"Section '{request.SectionName}' does not exist."
                            );
                            return;
                        }

                        // Check widget exists in section
                        var widget = entity
                            .Widgets[request.SectionName]
                            .FirstOrDefault(w => w.Id == request.WidgetId.Guid);

                        if (widget == null)
                        {
                            context.AddFailure(
                                "WidgetId",
                                $"Widget not found in section '{request.SectionName}'."
                            );
                            return;
                        }

                        // Validate settings JSON
                        if (!string.IsNullOrEmpty(request.SettingsJson))
                        {
                            try
                            {
                                JsonDocument.Parse(request.SettingsJson);
                            }
                            catch
                            {
                                context.AddFailure("SettingsJson", "Invalid JSON format.");
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
            var entity = _db.SitePages.First(p => p.Id == request.SitePageId.Guid);

            var widgets = entity.Widgets;
            var widget = widgets[request.SectionName].First(w => w.Id == request.WidgetId.Guid);

            // Update settings
            widget.SettingsJson = request.SettingsJson ?? "{}";

            // Save back
            entity.Widgets = widgets;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.WidgetId);
        }
    }
}

