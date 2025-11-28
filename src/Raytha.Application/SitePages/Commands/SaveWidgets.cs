using System.Text.Json;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.SitePages.Widgets;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages.Commands;

public class SaveWidgets
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        /// <summary>
        /// The section name containing the widgets (e.g., "main", "sidebar").
        /// </summary>
        public string SectionName { get; init; } = string.Empty;

        /// <summary>
        /// The widgets to save for this section.
        /// </summary>
        public IEnumerable<WidgetInput> Widgets { get; init; } = Array.Empty<WidgetInput>();
    }

    /// <summary>
    /// Input model for a widget instance.
    /// </summary>
    public record WidgetInput
    {
        /// <summary>
        /// The widget instance ID (empty/null for new widgets).
        /// </summary>
        public ShortGuid? Id { get; init; }

        /// <summary>
        /// The widget type developer name (e.g., "hero", "wysiwyg").
        /// </summary>
        public string WidgetType { get; init; } = string.Empty;

        /// <summary>
        /// JSON-serialized widget settings.
        /// </summary>
        public string SettingsJson { get; init; } = "{}";

        /// <summary>
        /// Row index for grid layout (0-indexed).
        /// </summary>
        public int Row { get; init; }

        /// <summary>
        /// Column start position within the row (0-11 for 12-column grid).
        /// </summary>
        public int Column { get; init; }

        /// <summary>
        /// Number of columns this widget spans (1-12).
        /// </summary>
        public int ColumnSpan { get; init; } = 12;

        /// <summary>
        /// Optional CSS class(es) to add to the widget wrapper element.
        /// </summary>
        public string CssClass { get; init; } = string.Empty;

        /// <summary>
        /// Optional HTML id attribute for the widget wrapper element.
        /// </summary>
        public string HtmlId { get; init; } = string.Empty;

        /// <summary>
        /// Optional custom HTML attributes as a string.
        /// </summary>
        public string CustomAttributes { get; init; } = string.Empty;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.SectionName)
                .NotEmpty()
                .WithMessage("Section name is required.");

            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db.SitePages.FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("Site Page", request.Id);

                        // Validate each widget
                        var index = 0;
                        foreach (var widget in request.Widgets)
                        {
                            if (string.IsNullOrEmpty(widget.WidgetType))
                            {
                                context.AddFailure(
                                    $"Widgets[{index}].WidgetType",
                                    "Widget type is required."
                                );
                            }
                            else if (!WidgetDefinitionService.IsValidWidgetType(widget.WidgetType))
                            {
                                context.AddFailure(
                                    $"Widgets[{index}].WidgetType",
                                    $"Widget type '{widget.WidgetType}' is not supported."
                                );
                            }

                            if (widget.ColumnSpan < 1 || widget.ColumnSpan > 12)
                            {
                                context.AddFailure(
                                    $"Widgets[{index}].ColumnSpan",
                                    "Column span must be between 1 and 12."
                                );
                            }

                            if (widget.Column < 0 || widget.Column > 11)
                            {
                                context.AddFailure(
                                    $"Widgets[{index}].Column",
                                    "Column must be between 0 and 11."
                                );
                            }

                            if (widget.Row < 0)
                            {
                                context.AddFailure(
                                    $"Widgets[{index}].Row",
                                    "Row must be 0 or greater."
                                );
                            }

                            // Validate settings JSON is valid JSON
                            if (!string.IsNullOrEmpty(widget.SettingsJson))
                            {
                                try
                                {
                                    JsonDocument.Parse(widget.SettingsJson);
                                }
                                catch
                                {
                                    context.AddFailure(
                                        $"Widgets[{index}].SettingsJson",
                                        "Invalid JSON format."
                                    );
                                }
                            }

                            index++;
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
            var entity = _db.SitePages.First(p => p.Id == request.Id.Guid);

            // Get current widgets - if we have a draft, use that; otherwise start from published
            Dictionary<string, List<SitePageWidget>> currentWidgets;
            if (entity.IsDraft && !string.IsNullOrEmpty(entity._DraftWidgetsJson))
            {
                currentWidgets = entity.DraftWidgets;
            }
            else
            {
                // Start draft from published content
                currentWidgets = entity.PublishedWidgets.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                );
            }

            // Convert input to domain entities
            var sectionWidgets = request
                .Widgets.Select(w => new SitePageWidget
                {
                    Id = w.Id?.Guid ?? Guid.NewGuid(),
                    WidgetType = w.WidgetType,
                    SettingsJson = w.SettingsJson ?? "{}",
                    Row = w.Row,
                    Column = w.Column,
                    ColumnSpan = w.ColumnSpan,
                    CssClass = w.CssClass ?? string.Empty,
                    HtmlId = w.HtmlId ?? string.Empty,
                    CustomAttributes = w.CustomAttributes ?? string.Empty,
                })
                .ToList();

            // Update the section
            currentWidgets[request.SectionName] = sectionWidgets;

            // Save to draft and mark as having draft changes
            entity.DraftWidgets = currentWidgets;
            entity.IsDraft = true;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}

