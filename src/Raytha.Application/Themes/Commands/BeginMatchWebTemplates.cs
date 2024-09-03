using System.Text.Json;
using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Themes.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.Commands;

public class BeginMatchWebTemplates
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public required IDictionary<string, string> MatchedWebTemplateDeveloperNames { get; init; }

        public static Command Empty() => new()
        {
            MatchedWebTemplateDeveloperNames = new Dictionary<string, string>(),
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, IMediator mediator)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                if (!db.Themes.Any(rt => rt.Id == request.Id.Guid))
                    throw new NotFoundException("Theme", request.Id);

                var activeThemeId = db.OrganizationSettings
                    .Select(os => os.ActiveThemeId)
                    .First();

                var activeThemeWebTemplates = db.WebTemplates
                    .Where(wt => wt.ThemeId == activeThemeId)
                    .Select(wt => wt.DeveloperName)
                    .ToArray();

                var selectedThemeWebTemplates = db.WebTemplates
                    .Where(wt => wt.ThemeId == request.Id.Guid)
                    .Select(wt => wt.DeveloperName)
                    .ToArray();

                foreach (var matchedTemplate in request.MatchedWebTemplateDeveloperNames)
                {
                    if (!activeThemeWebTemplates.Contains(matchedTemplate.Key))
                    {
                        context.AddFailure($"The template '{matchedTemplate.Key}' from the active theme was not found.");

                        return;
                    }

                    if (!selectedThemeWebTemplates.Contains(matchedTemplate.Value))
                    {
                        context.AddFailure($"The template '{matchedTemplate.Value}' from the selected theme was not found.");

                        return;
                    }
                }

                var webTemplateDeveloperNamesWithoutRelationResponse = mediator.Send(new GetWebTemplateDeveloperNamesWithoutRelation.Query
                {
                    ThemeId = request.Id,
                }).Result;

                if (!request.MatchedWebTemplateDeveloperNames.Keys.All(dv => webTemplateDeveloperNamesWithoutRelationResponse.Result.Contains(dv)))
                {
                    context.AddFailure("There are no templates in the request for which relations should be created");
                }
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IBackgroundTaskQueue _taskQueue;

        public Handler(IBackgroundTaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var backgroundJobId = await _taskQueue.EnqueueAsync<BackgroundTask>(request, cancellationToken);

            return new CommandResponseDto<ShortGuid>(backgroundJobId);
        }
    }

    public class BackgroundTask : IExecuteBackgroundTask
    {
        private readonly IRaythaDbContext _db;
        private readonly IMediator _mediator;

        public BackgroundTask(IRaythaDbContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            var selectedThemeId = args.GetProperty("Id").GetProperty("Guid").GetGuid();
            var matchedWebTemplates = JsonSerializer.Deserialize<IDictionary<string, string>>(args.GetProperty("MatchedWebTemplateDeveloperNames").GetRawText())!;

            var job = _db.BackgroundTasks.First(p => p.Id == jobId);
            job.TaskStep = 1;
            job.StatusInfo = "Setting selected templates for content items and views";
            job.PercentComplete = 0;
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var activeThemeWebTemplateContentItemRelations = await _db.WebTemplateContentItemRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == activeThemeId)
                .Select(wtr => new { WebTemplateDeveloperName = wtr.WebTemplate!.DeveloperName, wtr.ContentItemId })
                .ToArrayAsync(cancellationToken);

            var activeThemeWebTemplateViewRelations = await _db.WebTemplateViewRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == activeThemeId)
                .Select(wtr => new { WebTemplateDeveloperName = wtr.WebTemplate!.DeveloperName, wtr.ViewId })
                .ToArrayAsync(cancellationToken);

            var selectedThemeRelationContentItemIds = await _db.WebTemplateContentItemRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == selectedThemeId)
                .Select(wtr => wtr.ContentItemId)
                .ToArrayAsync(cancellationToken);

            var selectedThemeRelationViewIds = await _db.WebTemplateViewRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == selectedThemeId)
                .Select(wtr => wtr.ViewId)
                .ToArrayAsync(cancellationToken);

            var selectedThemeWebTemplateDeveloperNamesIds = await _db.WebTemplates
                .Where(wt => wt.ThemeId == selectedThemeId)
                .Select(wt => new { wt.Id, wt.DeveloperName })
                .ToDictionaryAsync(wt => wt.DeveloperName!, wt => wt.Id, cancellationToken);

            var currentIndex = 1;
            foreach (var matchedWebTemplate in matchedWebTemplates)
            {
                var selectedThemeWebTemplateId = selectedThemeWebTemplateDeveloperNamesIds[matchedWebTemplate.Value];

                var missingWebTemplateContentItemRelations = activeThemeWebTemplateContentItemRelations
                    .Where(wtr => wtr.WebTemplateDeveloperName == matchedWebTemplate.Key && !selectedThemeRelationContentItemIds.Contains(wtr.ContentItemId))
                    .ToArray();

                await _db.WebTemplateContentItemRelations.AddRangeAsync(missingWebTemplateContentItemRelations.Select(wtr => new WebTemplateContentItemRelation
                {
                    Id = Guid.NewGuid(),
                    ContentItemId = wtr.ContentItemId,
                    WebTemplateId = selectedThemeWebTemplateId,
                }), cancellationToken);

                var missingWebTemplateViewRelations = activeThemeWebTemplateViewRelations
                    .Where(wtr => wtr.WebTemplateDeveloperName == matchedWebTemplate.Key && !selectedThemeRelationViewIds.Contains(wtr.ViewId))
                    .ToArray();

                await _db.WebTemplateViewRelations.AddRangeAsync(missingWebTemplateViewRelations.Select(wtr => new WebTemplateViewRelation
                {
                    Id = Guid.NewGuid(),
                    ViewId = wtr.ViewId,
                    WebTemplateId = selectedThemeWebTemplateId,
                }), cancellationToken);

                job.StatusInfo = "Setting selected templates for content items and views.";
                job.PercentComplete = 100 * currentIndex / matchedWebTemplates.Count;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                currentIndex++;
            }

            job.StatusInfo = "Setting selected templates for content items and views has been completed.";
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Send(new SetAsActiveThemeInternal.Command
            {
                ThemeId = selectedThemeId,
            }, cancellationToken);

            job.TaskStep = 2;
            job.StatusInfo = "Setting the theme as an active theme is completed.";
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}