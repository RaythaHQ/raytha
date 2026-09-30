using System.Text.Json;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WidgetTemplates.Commands;

public class DeleteWidgetTemplate
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db
                            .WidgetTemplates.Select(wt => new { wt.Id, wt.DeveloperName })
                            .FirstOrDefault(wt => wt.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("Widget Template", request.Id);

                        if (BuiltInWidgetType.IsBuiltIn(entity.DeveloperName))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Built-in widget templates cannot be deleted. Reset them to default instead."
                            );
                            return;
                        }

                        // Pages reference widgets by developer name, not by theme, so any page using
                        // this name breaks while this theme is active.
                        var pagesUsingIt = db
                            .SitePages.Select(p => new
                            {
                                p.Title,
                                p._DraftWidgetsJson,
                                p._PublishedWidgetsJson,
                            })
                            .AsEnumerable()
                            .Where(p =>
                                UsesWidget(p._DraftWidgetsJson, entity.DeveloperName)
                                || UsesWidget(p._PublishedWidgetsJson, entity.DeveloperName)
                            )
                            .Select(p => p.Title)
                            .OrderBy(title => title)
                            .ToList();

                        if (pagesUsingIt.Count > 0)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                $"This widget template is used on these site pages: {string.Join(", ", pagesUsingIt)}. Remove those widgets before deleting it."
                            );
                        }
                    }
                );
        }

        private static bool UsesWidget(string? widgetsJson, string? developerName) =>
            !string.IsNullOrEmpty(widgetsJson)
            && (JsonSerializer.Deserialize<Dictionary<string, List<SitePageWidget>>>(widgetsJson) ?? [])
                .Values.SelectMany(section => section)
                .Any(w => w.WidgetType == developerName);
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
            var entity = await _db.WidgetTemplates.FirstAsync(
                wt => wt.Id == request.Id.Guid,
                cancellationToken
            );

            _db.WidgetTemplates.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
