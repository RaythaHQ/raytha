using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Http;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.Commands;

public class BeginDuplicateTheme
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid ThemeId { get; init; }
        public required string Title { get; init; }
        public required string DeveloperName { get; init; }
        public required string Description { get; init; }
        public required string PathBase { get; init; }

        public static Command Empty() =>
            new()
            {
                ThemeId = ShortGuid.Empty,
                Title = string.Empty,
                DeveloperName = string.Empty,
                Description = string.Empty,
                PathBase = string.Empty,
            };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.ThemeId).NotEqual(ShortGuid.Empty);
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Description).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (
                            db.Themes.Any(t =>
                                t.DeveloperName == request.DeveloperName.ToDeveloperName()
                            )
                        )
                            context.AddFailure(
                                "DeveloperName",
                                $"A theme with the developer name {request.DeveloperName.ToDeveloperName()} already exists."
                            );

                        if (!db.Themes.Any(t => t.Id == request.ThemeId.Guid))
                            throw new NotFoundException("Theme", request.ThemeId);
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IBackgroundTaskQueue _taskQueue;

        public Handler(IBackgroundTaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var backgroundJobId = await _taskQueue.EnqueueAsync<BackgroundTask>(
                request,
                cancellationToken
            );

            return new CommandResponseDto<ShortGuid>(backgroundJobId);
        }
    }

    public class BackgroundTask : IExecuteBackgroundTask
    {
        private readonly IRaythaDbContext _db;
        private readonly IFileStorageProvider _fileStorageProvider;
        private readonly IFileStorageProviderSettings _fileStorageSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public BackgroundTask(
            IRaythaDbContext db,
            IFileStorageProvider fileStorageProvider,
            IFileStorageProviderSettings fileStorageSettings,
            IHttpClientFactory httpClientFactory
        )
        {
            _db = db;
            _fileStorageProvider = fileStorageProvider;
            _fileStorageSettings = fileStorageSettings;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            ShortGuid themeId = args.GetProperty("ThemeId").GetString();
            var title = args.GetProperty("Title").GetString()!;
            var developerName = args.GetProperty("DeveloperName").GetString()!.ToDeveloperName();
            var description = args.GetProperty("Description").GetString()!;
            var pathBase = args.TryGetProperty("PathBase", out var pathBaseArg)
                ? pathBaseArg.GetString() ?? string.Empty
                : string.Empty;

            var job = await _db.BackgroundTasks.FirstAsync(p => p.Id == jobId, cancellationToken);

            job.TaskStep = 1;
            job.StatusInfo = "Beginning duplicate theme.";
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            var entity = new Theme
            {
                Id = Guid.NewGuid(),
                Title = title,
                DeveloperName = developerName.ToDeveloperName(),
                Description = description,
            };

            var mediaItems = new List<MediaItem>();
            var themeAccessToMediaItems = new List<ThemeAccessToMediaItem>();

            var originalThemeMediaItems = await _db
                .ThemeAccessToMediaItems.Where(tmi => tmi.ThemeId == themeId.Guid)
                .Select(tmi => tmi.MediaItem!)
                .ToArrayAsync(cancellationToken);

            var originalThemeWebTemplates = await _db
                .WebTemplates.AsNoTracking()
                .Include(wt => wt.TemplateAccessToModelDefinitions)
                .Include(wt => wt.ParentTemplate)
                .Where(wt => wt.ThemeId == themeId.Guid)
                .ToArrayAsync(cancellationToken);

            var originalThemeWidgetTemplates = await _db
                .WidgetTemplates.AsNoTracking()
                .Where(wt => wt.ThemeId == themeId.Guid)
                .ToArrayAsync(cancellationToken);

            // Source templates must stay untracked: a SaveChanges in the media loop would repoint the original theme at the copy's files.
            var duplicateWebTemplateContent = originalThemeWebTemplates.ToDictionary(
                wt => wt.Id,
                wt => wt.Content ?? string.Empty
            );

            job.TaskStep = 2;
            job.StatusInfo = "Duplicate media items";
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            var totalMediaItemsAndWebTemplates =
                originalThemeMediaItems.Length + originalThemeWebTemplates.Length;
            var currentIndex = 1;

            foreach (var originalThemeMediaItem in originalThemeMediaItems)
            {
                var id = ShortGuid.NewGuid();
                var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
                    id,
                    originalThemeMediaItem.FileName
                );
                var data = await ReadMediaBytes(originalThemeMediaItem.ObjectKey, pathBase, cancellationToken);
                await _fileStorageProvider.SaveAndGetDownloadUrlAsync(
                    data,
                    objectKey,
                    originalThemeMediaItem.FileName,
                    originalThemeMediaItem.ContentType,
                    FileStorageUtility.GetDefaultExpiry()
                );

                var mediaItem = new MediaItem
                {
                    Id = Guid.NewGuid(),
                    ObjectKey = objectKey,
                    ContentType = originalThemeMediaItem.ContentType,
                    FileName = originalThemeMediaItem.FileName,
                    FileStorageProvider = _fileStorageProvider.GetName(),
                    Length = originalThemeMediaItem.Length,
                };

                var themeAccessToMediaItem = new ThemeAccessToMediaItem
                {
                    Id = Guid.NewGuid(),
                    MediaItemId = mediaItem.Id,
                    ThemeId = entity.Id,
                };

                foreach (var templateId in duplicateWebTemplateContent.Keys.ToArray())
                {
                    var content = duplicateWebTemplateContent[templateId];
                    if (content.Contains(originalThemeMediaItem.ObjectKey))
                    {
                        duplicateWebTemplateContent[templateId] = content.Replace(
                            originalThemeMediaItem.ObjectKey,
                            mediaItem.ObjectKey
                        );
                    }
                }

                mediaItems.Add(mediaItem);
                themeAccessToMediaItems.Add(themeAccessToMediaItem);

                var percentDone = 100 * currentIndex / totalMediaItemsAndWebTemplates;
                job.PercentComplete = percentDone;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                currentIndex++;
            }

            var webTemplateDeveloperNamesWebTemplates = new Dictionary<string, WebTemplate>();
            var webTemplateContentItemRelations = new List<WebTemplateContentItemRelation>();
            var webTemplateViewRelations = new List<WebTemplateViewRelation>();

            var webTemplateIdsContentItemIds = await _db
                .WebTemplateContentItemRelations.Where(wtr =>
                    wtr.WebTemplate!.ThemeId == themeId.Guid
                )
                .Select(wtr => new { wtr.ContentItemId, wtr.WebTemplateId })
                .GroupBy(wtr => wtr.WebTemplateId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(wtr => wtr.ContentItemId).ToArray(),
                    cancellationToken
                );

            var webTemplateIdsViewIds = await _db
                .WebTemplateViewRelations.Where(wtr => wtr.WebTemplate!.ThemeId == themeId.Guid)
                .Select(wtr => new { wtr.WebTemplateId, wtr.ViewId })
                .GroupBy(wtr => wtr.WebTemplateId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(wtr => wtr.ViewId).ToArray(),
                    cancellationToken
                );

            job.TaskStep = 3;
            job.StatusInfo = "Duplicate web-templates";
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var originalThemeWebTemplate in originalThemeWebTemplates)
            {
                var webTemplateId = Guid.NewGuid();
                var webTemplate = new WebTemplate
                {
                    Id = webTemplateId,
                    ThemeId = entity.Id,
                    Label = originalThemeWebTemplate.Label,
                    DeveloperName = originalThemeWebTemplate.DeveloperName,
                    Content = duplicateWebTemplateContent[originalThemeWebTemplate.Id],
                    AllowAccessForNewContentTypes =
                        originalThemeWebTemplate.AllowAccessForNewContentTypes,
                    IsBaseLayout = originalThemeWebTemplate.IsBaseLayout,
                    IsBuiltInTemplate = originalThemeWebTemplate.IsBuiltInTemplate,
                    TemplateAccessToModelDefinitions =
                        originalThemeWebTemplate.TemplateAccessToModelDefinitions.Any()
                            ? originalThemeWebTemplate
                                .TemplateAccessToModelDefinitions.Select(
                                    md => new WebTemplateAccessToModelDefinition
                                    {
                                        Id = Guid.NewGuid(),
                                        ContentTypeId = md.ContentTypeId,
                                        WebTemplateId = webTemplateId,
                                    }
                                )
                                .ToArray()
                            : new List<WebTemplateAccessToModelDefinition>(),
                };

                if (
                    webTemplateIdsContentItemIds.TryGetValue(
                        originalThemeWebTemplate.Id,
                        out var contentItemIds
                    )
                )
                {
                    webTemplateContentItemRelations.AddRange(
                        contentItemIds.Select(contentItemId => new WebTemplateContentItemRelation
                        {
                            Id = Guid.NewGuid(),
                            ContentItemId = contentItemId,
                            WebTemplateId = webTemplateId,
                        })
                    );
                }

                if (webTemplateIdsViewIds.TryGetValue(originalThemeWebTemplate.Id, out var viewIds))
                {
                    webTemplateViewRelations.AddRange(
                        viewIds.Select(viewId => new WebTemplateViewRelation
                        {
                            Id = Guid.NewGuid(),
                            ViewId = viewId,
                            WebTemplateId = webTemplateId,
                        })
                    );
                }

                webTemplateDeveloperNamesWebTemplates.Add(webTemplate.DeveloperName!, webTemplate);

                var percentDone = 100 * currentIndex / totalMediaItemsAndWebTemplates;
                job.PercentComplete = percentDone;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                currentIndex++;
            }

            foreach (var originalThemeWebTemplate in originalThemeWebTemplates)
            {
                if (originalThemeWebTemplate.ParentTemplateId.HasValue)
                {
                    var webTemplate = webTemplateDeveloperNamesWebTemplates[
                        originalThemeWebTemplate.DeveloperName!
                    ];
                    var parentTemplateId = webTemplateDeveloperNamesWebTemplates[
                        originalThemeWebTemplate.ParentTemplate!.DeveloperName!
                    ].Id;

                    webTemplate.ParentTemplateId = parentTemplateId;
                }
            }

            await _db.Themes.AddAsync(entity, cancellationToken);
            await _db.MediaItems.AddRangeAsync(mediaItems, cancellationToken);
            await _db.ThemeAccessToMediaItems.AddRangeAsync(
                themeAccessToMediaItems,
                cancellationToken
            );
            await _db.WebTemplates.AddRangeAsync(
                webTemplateDeveloperNamesWebTemplates.Values,
                cancellationToken
            );
            await _db.WebTemplateContentItemRelations.AddRangeAsync(
                webTemplateContentItemRelations,
                cancellationToken
            );
            await _db.WebTemplateViewRelations.AddRangeAsync(
                webTemplateViewRelations,
                cancellationToken
            );

            // Duplicate widget templates
            var widgetTemplates = new List<WidgetTemplate>();
            foreach (var originalWidgetTemplate in originalThemeWidgetTemplates)
            {
                var widgetTemplate = new WidgetTemplate
                {
                    Id = Guid.NewGuid(),
                    ThemeId = entity.Id,
                    Label = originalWidgetTemplate.Label,
                    DeveloperName = originalWidgetTemplate.DeveloperName,
                    Content = originalWidgetTemplate.Content,
                    IsBuiltInTemplate = originalWidgetTemplate.IsBuiltInTemplate,
                };
                widgetTemplates.Add(widgetTemplate);
            }

            await _db.WidgetTemplates.AddRangeAsync(widgetTemplates, cancellationToken);

            job.TaskStep = 4;
            job.StatusInfo = "Duplicate theme is finished.";
            _db.BackgroundTasks.Update(job);

            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<byte[]> ReadMediaBytes(
            string objectKey,
            string pathBase,
            CancellationToken cancellationToken
        )
        {
            if (_fileStorageProvider.GetName() == FileStorageUtility.LOCAL)
            {
                var filePath = Path.Combine(_fileStorageSettings.LocalDirectory, objectKey);
                if (File.Exists(filePath))
                {
                    return await File.ReadAllBytesAsync(filePath, cancellationToken);
                }
            }

            var downloadUrl = await _fileStorageProvider.GetDownloadUrlAsync(
                objectKey,
                FileStorageUtility.GetDefaultExpiry()
            );
            if (!downloadUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var prefix = pathBase.TrimEnd('/');
                var path = downloadUrl.StartsWith('/') ? downloadUrl : $"/{downloadUrl}";
                downloadUrl = $"{prefix}{path}";
            }

            var client = _httpClientFactory.CreateClient(nameof(BeginDuplicateTheme));
            var fileInfo = await FileDownloadUtility.DownloadFile(client, downloadUrl, cancellationToken);
            if (fileInfo == null)
            {
                throw new Exception($"Unable to download file from {downloadUrl}.");
            }
            return fileInfo.FileMemoryStream.ToArray();
        }
    }
}
