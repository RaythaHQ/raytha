using System.Text.Json;
using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.Commands;

public class BeginImportThemeFromUrl
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required string Title { get; init; }
        public required string DeveloperName { get; init; }
        public required string Description { get; init; }
        public required string Url { get; init; }

        public static Command Empty() => new()
        {
            Title = string.Empty,
            DeveloperName = string.Empty,
            Description = string.Empty,
            Url = string.Empty,
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Description).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x.Url).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (db.Themes.Any(t => t.DeveloperName == request.DeveloperName.ToDeveloperName()))
                    context.AddFailure("DeveloperName", $"A theme with the developer name {request.DeveloperName.ToDeveloperName()} already exists.");

                if (!request.Url.IsValidUriFormat())
                    context.AddFailure("Url", $"Invalid url format: {request.Url}");
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
        private readonly IFileStorageProvider _fileStorageProvider;

        private static readonly HttpClient _httpClient = new HttpClient();

        public BackgroundTask(IRaythaDbContext db, IFileStorageProvider fileStorageProvider)
        {
            _db = db;
            _fileStorageProvider = fileStorageProvider;
        }

        public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            var urlToDownloadJson = args.GetProperty("Url").GetString()!;
            var title = args.GetProperty("Title").GetString()!;
            var developerName = args.GetProperty("DeveloperName").GetString()!.ToDeveloperName();
            var description = args.GetProperty("Description").GetString()!;

            // download json and deserialize
            var job = _db.BackgroundTasks.First(p => p.Id == jobId);

            job.TaskStep = 1;
            job.StatusInfo = "Beginning import. Downloading theme json package from the url.";
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            try
            {
                var themePackageJson = await GetJsonFromUrl(urlToDownloadJson, cancellationToken);
                var themePackage = JsonSerializer.Deserialize<ThemeJson>(themePackageJson)!;

                job.TaskStep = 2;
                job.StatusInfo = $"Importing theme - {title}";
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                var themeId = Guid.NewGuid();
                var theme = new Theme
                {
                    Id = themeId,
                    Title = title,
                    DeveloperName = developerName,
                    Description = description,
                };

                // importing web-templates
                job.TaskStep = 3;
                job.StatusInfo = "Importing web-templates";
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                var contentTypeIds = await _db.ContentTypes
                    .Select(ct => ct.Id)
                    .ToArrayAsync(cancellationToken);

                var webTemplateDeveloperNamesWebTemplates = new Dictionary<string, WebTemplate>();

                foreach (var webTemplateFromJson in themePackage.WebTemplates)
                {
                    var webTemplateId = Guid.NewGuid();
                    var webTemplate = new WebTemplate
                    {
                        Id = Guid.NewGuid(),
                        ThemeId = themeId,
                        DeveloperName = webTemplateFromJson.DeveloperName,
                        Label = webTemplateFromJson.Label,
                        Content = webTemplateFromJson.Content,
                        IsBaseLayout = webTemplateFromJson.IsBaseLayout,
                        IsBuiltInTemplate = webTemplateFromJson.IsBuiltInTemplate,
                        AllowAccessForNewContentTypes = webTemplateFromJson.AllowAccessForNewContentTypes,
                        TemplateAccessToModelDefinitions = webTemplateFromJson.AllowAccessForNewContentTypes
                            ? contentTypeIds.Select(id => new WebTemplateAccessToModelDefinition { Id = Guid.NewGuid(), ContentTypeId = id, WebTemplateId = webTemplateId}).ToArray()
                            : new List<WebTemplateAccessToModelDefinition>(),
                    };

                    webTemplateDeveloperNamesWebTemplates.Add(webTemplate.DeveloperName, webTemplate);
                }

                foreach (var themePackageWebTemplate in themePackage.WebTemplates)
                {
                    if (!string.IsNullOrEmpty(themePackageWebTemplate.ParentTemplateDeveloperName))
                    {
                        var webTemplate = webTemplateDeveloperNamesWebTemplates[themePackageWebTemplate.DeveloperName];
                        var parentTemplateId = webTemplateDeveloperNamesWebTemplates[themePackageWebTemplate.ParentTemplateDeveloperName].Id;

                        webTemplate.ParentTemplateId = parentTemplateId;
                    }
                }

                // importing media items and download files
                var mediaItems = new List<MediaItem>();
                var themeAccessToMediaItems = new List<ThemeAccessToMediaItem>();

                job.TaskStep = 4;
                job.StatusInfo = "Importing media items and downloading files";
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);

                var totalMediaItems = themePackage.MediaItems.Count();
                var currentMediaItemIndex = 1;

                foreach (var mediaItemInThemePackage in themePackage.MediaItems)
                {
                    var data = await GetDataFromUrl(mediaItemInThemePackage.DownloadUrl, cancellationToken);
                    var contentType = FileStorageUtility.GetMimeType(mediaItemInThemePackage.FileName);
                    var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(theme.DeveloperName, mediaItemInThemePackage.FileName);

                    await _fileStorageProvider.SaveAndGetDownloadUrlAsync(data, objectKey, mediaItemInThemePackage.FileName, contentType, FileStorageUtility.GetDefaultExpiry());

                    var mediaItem = new MediaItem
                    {
                        Id = Guid.NewGuid(),
                        FileName = mediaItemInThemePackage.FileName,
                        Length = data.Length,
                        ContentType = contentType,
                        ObjectKey = objectKey,
                        FileStorageProvider = _fileStorageProvider.GetName(),
                    };

                    var themeAccessToMediaItem = new ThemeAccessToMediaItem
                    {
                        Id = Guid.NewGuid(),
                        MediaItemId = mediaItem.Id,
                        ThemeId = theme.Id,
                    };

                    mediaItems.Add(mediaItem);
                    themeAccessToMediaItems.Add(themeAccessToMediaItem);

                    var percentDone = 100 * currentMediaItemIndex / totalMediaItems;
                    job.PercentComplete = percentDone;
                    _db.BackgroundTasks.Update(job);
                    await _db.SaveChangesAsync(cancellationToken);

                    currentMediaItemIndex++;
                }

                await _db.Themes.AddAsync(theme, cancellationToken);
                await _db.WebTemplates.AddRangeAsync(webTemplateDeveloperNamesWebTemplates.Values, cancellationToken);
                await _db.MediaItems.AddRangeAsync(mediaItems, cancellationToken);
                await _db.ThemeAccessToMediaItems.AddRangeAsync(themeAccessToMediaItems, cancellationToken);

                job.TaskStep = 5;
                job.StatusInfo = "Finished importing.";
                job.PercentComplete = 100;
                _db.BackgroundTasks.Update(job);

                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                job.ErrorMessage = $"Failed to import. {e.Message}";
                job.Status = BackgroundTaskStatus.Error;
                _db.BackgroundTasks.Update(job);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<string> GetJsonFromUrl(string url, CancellationToken cancellationToken)
        {
            var content = await GetContentByUrl(url, cancellationToken);

            return await content.ReadAsStringAsync(cancellationToken);
        }

        private async Task<byte[]> GetDataFromUrl(string url, CancellationToken cancellationToken)
        {
            var content = await GetContentByUrl(url, cancellationToken);

            return await content.ReadAsByteArrayAsync(cancellationToken);
        }

        private async Task<HttpContent> GetContentByUrl(string urlToDownload, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(urlToDownload, cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Unable to retrieve file from {urlToDownload}: {response.StatusCode} - {response.ReasonPhrase}");

            return response.Content;
        }
    }
}