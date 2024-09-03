using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.Commands;

public class CreateTheme
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required string Title { get; init; }
        public required string DeveloperName { get; init; }
        public required string Description { get; init; }
        public required bool InsertDefaultThemeMediaItems { get; init; }

        public static Command Empty() => new()
        {
            Title = string.Empty,
            DeveloperName = string.Empty,
            Description = string.Empty,
            InsertDefaultThemeMediaItems = false,
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Title).NotEmpty();
            RuleFor(x => x.Description).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (db.Themes.Any(t => t.DeveloperName == request.DeveloperName.ToDeveloperName()))
                    context.AddFailure("DeveloperName", $"A theme with the developer name {request.DeveloperName.ToDeveloperName()} already exists.");
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IFileStorageProvider _fileStorageProvider;

        public Handler(IRaythaDbContext db, IFileStorageProvider fileStorageProvider)
        {
            _db = db;
            _fileStorageProvider = fileStorageProvider;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var themeId = Guid.NewGuid();
            var theme = new Theme
            {
                Id = themeId,
                Title = request.Title,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
                Description = request.Description,
            };

            await _db.Themes.AddAsync(theme, cancellationToken);

            IReadOnlyCollection<MediaItem> mediaItems = new List<MediaItem>();
            if (request.InsertDefaultThemeMediaItems)
                mediaItems = await InsertDefaultMediaItemsAsync(themeId, cancellationToken);

            await InsertDefaultWebTemplates(themeId, mediaItems, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(theme.Id);
        }

        private async Task<IReadOnlyCollection<MediaItem>> InsertDefaultMediaItemsAsync(Guid themeId, CancellationToken cancellationToken)
        {
            var mediaItems = new List<MediaItem>();
            var themeAccessToMediaItems = new List<ThemeAccessToMediaItem>();

            var defaultThemeAssetsPath = Path.Combine("wwwroot", "raytha_default_2024", "assets");
            if (!Directory.Exists(defaultThemeAssetsPath))
                throw new DirectoryNotFoundException($"Path '{defaultThemeAssetsPath}' does not exist.");

            var themeFiles = Directory.GetFiles(defaultThemeAssetsPath, "*", SearchOption.AllDirectories);
            foreach (var file in themeFiles)
            {
                var idForKey = ShortGuid.NewGuid();
                var fileName = Path.GetFileName(file);
                var data = await File.ReadAllBytesAsync(file, cancellationToken);
                var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(idForKey, fileName);
                var contentType = FileStorageUtility.GetMimeType(fileName);

                await _fileStorageProvider.SaveAndGetDownloadUrlAsync(data, objectKey, fileName, contentType, FileStorageUtility.GetDefaultExpiry());

                var mediaItem = new MediaItem
                {
                    Id = Guid.NewGuid(),
                    FileName = fileName,
                    FileStorageProvider = _fileStorageProvider.GetName(),
                    ObjectKey = objectKey,
                    Length = data.Length,
                    ContentType = contentType,
                };

                mediaItems.Add(mediaItem);

                themeAccessToMediaItems.Add(new ThemeAccessToMediaItem
                {
                    Id = Guid.NewGuid(),
                    ThemeId = themeId,
                    MediaItemId = mediaItem.Id,
                });
            }

            await _db.MediaItems.AddRangeAsync(mediaItems, cancellationToken);
            await _db.ThemeAccessToMediaItems.AddRangeAsync(themeAccessToMediaItems, cancellationToken);

            return mediaItems;
        }

        private async Task InsertDefaultWebTemplates(Guid themeId, IReadOnlyCollection<MediaItem> mediaItems, CancellationToken cancellationToken)
        {
            var loginWebTemplates = new List<string>
            {
                BuiltInWebTemplate.LoginWithEmailAndPasswordPage,
                BuiltInWebTemplate.LoginWithMagicLinkPage,
                BuiltInWebTemplate.LoginWithMagicLinkSentPage,
                BuiltInWebTemplate.ForgotPasswordPage,
                BuiltInWebTemplate.ForgotPasswordCompletePage,
                BuiltInWebTemplate.ForgotPasswordResetLinkSentPage,
                BuiltInWebTemplate.ForgotPasswordSuccessPage,
                BuiltInWebTemplate.UserRegistrationForm,
                BuiltInWebTemplate.UserRegistrationFormSuccess,
                BuiltInWebTemplate.ChangePasswordPage,
                BuiltInWebTemplate.ChangeProfilePage
            };

            var standardWebTemplatesForContentTypes = new List<string>
            {
                BuiltInWebTemplate.HomePage,
                BuiltInWebTemplate.ContentItemDetailViewPage,
                BuiltInWebTemplate.ContentItemListViewPage
            };

            var defaultWebTemplates = new List<WebTemplate>();
            var defaultBaseLayout = BuiltInWebTemplate._Layout;
            var defaultBaseContent = defaultBaseLayout.DefaultContent;
            var insertDefaultMediaItems = mediaItems.Count > 0;

            if (insertDefaultMediaItems)
            {
                var baseLayoutFileNames = new[]
                {
                    "favicon.ico",
                    "bootstrap.min.css",
                    "bootstrap.bundle.min.js",
                };

                foreach (var fileName in baseLayoutFileNames)
                {
                    var mediaItem = mediaItems.First(mi => mi.FileName.Contains(fileName));
                    defaultBaseContent = defaultBaseContent.Replace(fileName, mediaItem.ObjectKey);
                }
            }

            var baseLayout = new WebTemplate
            {
                Id = Guid.NewGuid(),
                ThemeId = themeId,
                IsBaseLayout = true,
                IsBuiltInTemplate = true,
                Content = defaultBaseContent,
                Label = defaultBaseLayout.DefaultLabel,
                DeveloperName = defaultBaseLayout.DeveloperName,
            };

            defaultWebTemplates.Add(baseLayout);

            var defaultBaseLoginLayout = BuiltInWebTemplate._LoginLayout;
            var baseLoginLayout = new WebTemplate
            {
                Id = Guid.NewGuid(),
                ThemeId = themeId,
                IsBaseLayout = true,
                IsBuiltInTemplate = true,
                Content = defaultBaseLoginLayout.DefaultContent,
                Label = defaultBaseLoginLayout.DefaultLabel,
                DeveloperName = defaultBaseLoginLayout.DeveloperName,
                ParentTemplateId = baseLayout.Id
            };

            defaultWebTemplates.Add(baseLoginLayout);

            var builtInWebTemplatesWithoutBaseLayout = BuiltInWebTemplate.Templates
                .Where(bwt => bwt.DeveloperName != BuiltInWebTemplate._Layout.DeveloperName && bwt.DeveloperName != BuiltInWebTemplate._LoginLayout.DeveloperName)
                .ToArray();

            var contentTypeIds = await _db.ContentTypes
                .Select(ct => ct.Id)
                .ToArrayAsync(cancellationToken);

            foreach (var webTemplateToBuild in builtInWebTemplatesWithoutBaseLayout)
            {
                var webTemplate = new WebTemplate
                {
                    Id = Guid.NewGuid(),
                    ThemeId = themeId,
                    ParentTemplateId = baseLayout.Id,
                    IsBaseLayout = false,
                    IsBuiltInTemplate = true,
                    Label = webTemplateToBuild.DefaultLabel,
                    DeveloperName = webTemplateToBuild.DeveloperName,
                    Content = webTemplateToBuild.DefaultContent,
                };

                if (standardWebTemplatesForContentTypes.Contains(webTemplateToBuild))
                {
                    webTemplate.IsBuiltInTemplate = false;
                    webTemplate.AllowAccessForNewContentTypes = true;
                    webTemplate.TemplateAccessToModelDefinitions = contentTypeIds
                        .Select(contentTypeId => new WebTemplateAccessToModelDefinition { ContentTypeId = contentTypeId })
                        .ToList();

                    if (webTemplate.DeveloperName == BuiltInWebTemplate.HomePage.DeveloperName && insertDefaultMediaItems)
                    {
                        const string fileName = "raythadotcom_screenshot.webp";
                        var mediaItem = mediaItems.First(mi => mi.FileName.Contains(fileName));

                        webTemplate.Content = webTemplate.Content.Replace(fileName, mediaItem.ObjectKey);
                    }
                }
                else if (loginWebTemplates.Contains(webTemplateToBuild))
                {
                    webTemplate.ParentTemplateId = baseLoginLayout.Id;
                }

                defaultWebTemplates.Add(webTemplate);
            }

            await _db.WebTemplates.AddRangeAsync(defaultWebTemplates, cancellationToken);
        }
    }
}