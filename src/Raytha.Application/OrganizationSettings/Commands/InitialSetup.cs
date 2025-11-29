using System.Dynamic;
using System.Text.Json.Serialization;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.OrganizationSettings.Commands;

public class InitialSetup
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;

        [JsonIgnore]
        public string SuperAdminEmailAddress { get; init; } = null!;

        [JsonIgnore]
        public string SuperAdminPassword { get; init; } = null!;
        public string OrganizationName { get; init; } = null!;
        public string WebsiteUrl { get; init; } = null!;
        public string TimeZone { get; init; } = null!;
        public string SmtpDefaultFromAddress { get; init; } = null!;
        public string SmtpDefaultFromName { get; init; } = null!;

        [JsonIgnore]
        public string SmtpHost { get; init; } = null!;

        [JsonIgnore]
        public int? SmtpPort { get; init; }

        [JsonIgnore]
        public string SmtpUsername { get; init; } = null!;

        [JsonIgnore]
        public string SmtpPassword { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IEmailerConfiguration emailerConfiguration)
        {
            RuleFor(x => x.SuperAdminPassword).NotEmpty().MinimumLength(8);
            RuleFor(x => x.SuperAdminEmailAddress).EmailAddress();
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.SmtpPort)
                .GreaterThan(0)
                .LessThanOrEqualTo(65535)
                .When(p => p.SmtpPort.HasValue);
            RuleFor(x => x.OrganizationName).NotEmpty();
            RuleFor(x => x.TimeZone)
                .Must(DateTimeExtensions.IsValidTimeZone)
                .WithMessage(p => $"{p.TimeZone} timezone is unrecognized.");
            RuleFor(x => x.WebsiteUrl)
                .Must(StringExtensions.IsValidUriFormat)
                .WithMessage(p => $"{p.WebsiteUrl} must be a valid URI format.");
            RuleFor(x => x.SmtpDefaultFromAddress).EmailAddress();
            RuleFor(x => x.SmtpDefaultFromName).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private const string POSTS_NAME_PLURAL = "Posts";
        private const string POSTS_NAME_SINGULAR = "Post";
        private const string POSTS_DEVELOPER_NAME = "posts";

        private const string TITLE_FIELD_LABEL = "Title";
        private const string TITLE_FIELD_DEVELOPER_NAME = "title";
        private const string CONTENT_FIELD_LABEL = "Content";
        private const string CONTENT_FIELD_DEVELOPER_NAME = "content";
        private const string FEATURED_IMAGE_FIELD_LABEL = "Featured Image";
        private const string FEATURED_IMAGE_FIELD_DEVELOPER_NAME = "featured_image";

        private const string DEFAULT_THEME_DEVELOPER_NAME = "raytha_default_2026";

        Guid postTypeGuid = Guid.NewGuid();
        Guid postsTitleFieldGuid = Guid.NewGuid();
        Guid homeSitePageGuid = Guid.NewGuid();
        Guid aboutSitePageGuid = Guid.NewGuid();
        Guid orgSettingsGuid = Guid.NewGuid();

        private readonly IRaythaDbContext _db;
        private readonly IEmailerConfiguration _emailerConfiguration;
        private readonly IFileStorageProvider _fileStorageProvider;

        public Handler(
            IRaythaDbContext db,
            IEmailerConfiguration emailerConfiguration,
            IFileStorageProvider fileStorageProvider
        )
        {
            _db = db;
            _emailerConfiguration = emailerConfiguration;
            _fileStorageProvider = fileStorageProvider;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var defaultThemeId = await _db.Themes.Select(t => t.Id).FirstAsync(cancellationToken);

            InsertOrganizationSettings(request, defaultThemeId);
            InsertDefaultContentTypes();
            InsertDefaultContentTypeFields();
            InsertDefaultRolesAndSuperAdmin(request);

            var mediaItemObjectKeys = await InsertDefaultThemeAssetsToFileStorage(
                cancellationToken,
                defaultThemeId
            );
            InsertDefaultWebTemplates(mediaItemObjectKeys, defaultThemeId);
            InsertDefaultWidgetTemplates(defaultThemeId);
            InsertDefaultEmailTemplates();
            InsertDefaultAuthentications();
            InsertDefaultNavigationMenu();
            await _db.SaveChangesAsync(cancellationToken);

            InsertDefaultSitePages(defaultThemeId);
            InsertDefaultPosts();
            InsertDefaultViews();
            SetPrimaryFieldsOnContentTypes();
            SetHomePage();
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(orgSettingsGuid);
        }

        protected void InsertOrganizationSettings(Command request, Guid defaultThemeId)
        {
            var entity = new Domain.Entities.OrganizationSettings
            {
                Id = orgSettingsGuid,
                SmtpHost = request.SmtpHost,
                SmtpPort = request.SmtpPort,
                SmtpUsername = request.SmtpUsername,
                SmtpPassword = request.SmtpPassword,
                SmtpOverrideSystem = _emailerConfiguration.IsMissingSmtpEnvVars(),
                OrganizationName = request.OrganizationName,
                TimeZone = request.TimeZone,
                DateFormat = DateTimeExtensions.DEFAULT_DATE_FORMAT,
                WebsiteUrl = request.WebsiteUrl,
                SmtpDefaultFromAddress = request.SmtpDefaultFromAddress,
                SmtpDefaultFromName = request.SmtpDefaultFromName,
                ActiveThemeId = defaultThemeId,
            };
            _db.OrganizationSettings.Add(entity);
        }

        protected void InsertDefaultRolesAndSuperAdmin(Command request)
        {
            var roles = new List<Role>();
            Role superAdminRole = new Role
            {
                Id = Guid.NewGuid(),
                Label = BuiltInRole.SuperAdmin.DefaultLabel,
                DeveloperName = BuiltInRole.SuperAdmin,
                SystemPermissions = BuiltInRole.SuperAdmin.DefaultSystemPermission,
                ContentTypeRolePermissions = new List<ContentTypeRolePermission>
                {
                    new ContentTypeRolePermission
                    {
                        ContentTypeId = postTypeGuid,
                        ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum,
                    },
                },
                CreationTime = DateTime.UtcNow,
            };
            roles.Add(superAdminRole);
            Role adminRole = new Role
            {
                Id = Guid.NewGuid(),
                Label = BuiltInRole.Admin.DefaultLabel,
                DeveloperName = BuiltInRole.Admin,
                SystemPermissions = BuiltInRole.Admin.DefaultSystemPermission,
                ContentTypeRolePermissions = new List<ContentTypeRolePermission>
                {
                    new ContentTypeRolePermission
                    {
                        ContentTypeId = postTypeGuid,
                        ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum,
                    },
                },
                CreationTime = DateTime.UtcNow,
            };
            roles.Add(adminRole);
            Role editorRole = new Role
            {
                Id = Guid.NewGuid(),
                Label = BuiltInRole.Editor.DefaultLabel,
                DeveloperName = BuiltInRole.Editor,
                SystemPermissions = BuiltInRole.Editor.DefaultSystemPermission,
                ContentTypeRolePermissions = new List<ContentTypeRolePermission>
                {
                    new ContentTypeRolePermission
                    {
                        ContentTypeId = postTypeGuid,
                        ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum,
                    },
                },
                CreationTime = DateTime.UtcNow,
            };
            roles.Add(editorRole);

            var salt = PasswordUtility.RandomSalt();
            var superAdmin = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.SuperAdminEmailAddress,
                Roles = roles,
                Salt = salt,
                PasswordHash = PasswordUtility.Hash(request.SuperAdminPassword, salt),
                IsActive = true,
                IsAdmin = true,
            };
            _db.Users.Add(superAdmin);
        }

        protected void InsertDefaultContentTypeFields()
        {
            var titlePostField = new ContentTypeField
            {
                Id = postsTitleFieldGuid,
                Label = TITLE_FIELD_LABEL,
                DeveloperName = TITLE_FIELD_DEVELOPER_NAME,
                ContentTypeId = postTypeGuid,
                FieldOrder = 1,
                FieldType = BaseFieldType.SingleLineText,
            };
            _db.ContentTypeFields.Add(titlePostField);

            var contentPostField = new ContentTypeField
            {
                Id = Guid.NewGuid(),
                Label = CONTENT_FIELD_LABEL,
                DeveloperName = CONTENT_FIELD_DEVELOPER_NAME,
                ContentTypeId = postTypeGuid,
                FieldOrder = 2,
                FieldType = BaseFieldType.Wysiwyg,
            };
            _db.ContentTypeFields.Add(contentPostField);

            var featuredImageField = new ContentTypeField
            {
                Id = Guid.NewGuid(),
                Label = FEATURED_IMAGE_FIELD_LABEL,
                DeveloperName = FEATURED_IMAGE_FIELD_DEVELOPER_NAME,
                ContentTypeId = postTypeGuid,
                FieldOrder = 3,
                FieldType = BaseFieldType.Attachment,
            };
            _db.ContentTypeFields.Add(featuredImageField);
        }

        protected void InsertDefaultContentTypes()
        {
            var postContentType = new ContentType
            {
                Id = postTypeGuid,
                LabelPlural = POSTS_NAME_PLURAL,
                LabelSingular = POSTS_NAME_SINGULAR,
                DeveloperName = POSTS_DEVELOPER_NAME,
                IsActive = true,
                IsDeleted = false,
                DefaultRouteTemplate = "{CurrentYear}/{CurrentMonth}/{PrimaryField}",
            };
            _db.ContentTypes.Add(postContentType);
        }

        protected async Task<IReadOnlyCollection<MediaItem>> InsertDefaultThemeAssetsToFileStorage(
            CancellationToken cancellationToken,
            Guid defaultThemeId
        )
        {
            var mediaItems = new List<MediaItem>();
            var themeAccessToMediaItems = new List<ThemeAccessToMediaItem>();

            var defaultThemeAssetsPath = Path.Combine(
                "wwwroot",
                DEFAULT_THEME_DEVELOPER_NAME,
                "assets"
            );
            if (!Directory.Exists(defaultThemeAssetsPath))
                throw new DirectoryNotFoundException(
                    $"Path '{defaultThemeAssetsPath}' does not exist."
                );

            var themeFiles = Directory.GetFiles(
                defaultThemeAssetsPath,
                "*",
                SearchOption.AllDirectories
            );

            // Filter out compressed files (.br, .gz) that are generated during build/publish
            var excludedExtensions = new[] { ".br", ".gz" };
            themeFiles = themeFiles
                .Where(f => !excludedExtensions.Contains(Path.GetExtension(f)))
                .ToArray();

            foreach (var file in themeFiles)
            {
                var idForKey = ShortGuid.NewGuid();
                var fileName = Path.GetFileName(file);
                var data = await File.ReadAllBytesAsync(file, cancellationToken);
                var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
                    idForKey,
                    fileName
                );
                var contentType = FileStorageUtility.GetMimeType(fileName);

                await _fileStorageProvider.SaveAndGetDownloadUrlAsync(
                    data,
                    objectKey,
                    fileName,
                    contentType,
                    FileStorageUtility.GetDefaultExpiry()
                );

                var mediaItem = new MediaItem
                {
                    Id = idForKey.Guid,
                    FileName = fileName,
                    FileStorageProvider = _fileStorageProvider.GetName(),
                    ObjectKey = objectKey,
                    Length = data.Length,
                    ContentType = contentType,
                };

                mediaItems.Add(mediaItem);

                themeAccessToMediaItems.Add(
                    new ThemeAccessToMediaItem
                    {
                        Id = Guid.NewGuid(),
                        ThemeId = defaultThemeId,
                        MediaItemId = idForKey.Guid,
                    }
                );
            }

            _db.MediaItems.AddRange(mediaItems);
            _db.ThemeAccessToMediaItems.AddRange(themeAccessToMediaItems);

            return mediaItems;
        }

        protected void InsertDefaultWebTemplates(
            IReadOnlyCollection<MediaItem> mediaItems,
            Guid defaultThemeId
        )
        {
            var baseLayoutFileNames = new[]
            {
                "favicon.ico",
                "bootstrap.min.css",
                "bootstrap.bundle.min.js",
                "bootstrap-icons.min.css",
                "bootstrap-icons.woff2",
                "bootstrap-icons.woff",
            };

            var list = new List<WebTemplate>();
            var defaultBaseLayout = BuiltInWebTemplate._Layout;
            var updatedContent = defaultBaseLayout.DefaultContent;
            foreach (var fileName in baseLayoutFileNames)
            {
                var mediaItem = mediaItems.First(mi => mi.FileName.Contains(fileName));
                updatedContent = updatedContent.Replace(fileName, mediaItem.ObjectKey);
            }

            var baseLayout = new WebTemplate
            {
                Id = Guid.NewGuid(),
                ThemeId = defaultThemeId,
                IsBaseLayout = true,
                IsBuiltInTemplate = true,
                Content = updatedContent,
                Label = defaultBaseLayout.DefaultLabel,
                DeveloperName = defaultBaseLayout.DeveloperName,
            };
            list.Add(baseLayout);

            var defaultBaseLoginLayout = BuiltInWebTemplate._LoginLayout;
            var baseLoginLayout = new WebTemplate
            {
                Id = Guid.NewGuid(),
                ThemeId = defaultThemeId,
                IsBaseLayout = true,
                IsBuiltInTemplate = true,
                Content = defaultBaseLoginLayout.DefaultContent,
                Label = defaultBaseLoginLayout.DefaultLabel,
                DeveloperName = defaultBaseLoginLayout.DeveloperName,
                ParentTemplateId = baseLayout.Id,
            };
            list.Add(baseLoginLayout);

            var homePageMediaItem = "raythadotcom_screenshot.webp";

            foreach (
                var templateToBuild in BuiltInWebTemplate.Templates.Where(p =>
                    p.DeveloperName != BuiltInWebTemplate._Layout.DeveloperName
                    && p.DeveloperName != BuiltInWebTemplate._LoginLayout.DeveloperName
                )
            )
            {
                var template = new WebTemplate
                {
                    Id = Guid.NewGuid(),
                    ThemeId = defaultThemeId,
                    ParentTemplateId = baseLayout.Id,
                    IsBaseLayout = false,
                    IsBuiltInTemplate = true,
                    Content = templateToBuild.DefaultContent,
                    Label = templateToBuild.DefaultLabel,
                    DeveloperName = templateToBuild.DeveloperName,
                };

                var templateAccess = new List<WebTemplateAccessToModelDefinition>
                {
                    new WebTemplateAccessToModelDefinition { ContentTypeId = postTypeGuid },
                };

                var standardTemplatesForContentTypes = new List<string>
                {
                    BuiltInWebTemplate.ContentItemDetailViewPage,
                    BuiltInWebTemplate.ContentItemListViewPage,
                };

                var sitePageTemplates = new List<string>
                {
                    BuiltInWebTemplate.HomePage,
                    BuiltInWebTemplate.PageFullWidth,
                    BuiltInWebTemplate.PageSidebar,
                    BuiltInWebTemplate.PageMultiSection,
                };

                var loginTemplates = new List<string>
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
                    BuiltInWebTemplate.ChangeProfilePage,
                };

                if (standardTemplatesForContentTypes.Contains(templateToBuild))
                {
                    template.TemplateAccessToModelDefinitions = templateAccess;
                    template.IsBuiltInTemplate = false;
                    template.AllowAccessForNewContentTypes = true;
                }
                else if (sitePageTemplates.Contains(templateToBuild))
                {
                    // Site Page templates - not linked to content types
                    template.IsBuiltInTemplate = false;
                }
                else if (loginTemplates.Contains(templateToBuild))
                {
                    template.ParentTemplateId = baseLoginLayout.Id;
                }

                list.Add(template);
            }

            _db.WebTemplates.AddRange(list);
        }

        protected void InsertDefaultWidgetTemplates(Guid defaultThemeId)
        {
            var list = new List<WidgetTemplate>();

            foreach (var widgetType in BuiltInWidgetType.WidgetTypes)
            {
                var template = new WidgetTemplate
                {
                    Id = Guid.NewGuid(),
                    ThemeId = defaultThemeId,
                    Label = widgetType.DisplayName,
                    DeveloperName = widgetType.DeveloperName,
                    Content = widgetType.DefaultTemplateContent,
                    IsBuiltInTemplate = true,
                };
                list.Add(template);
            }

            _db.WidgetTemplates.AddRange(list);
        }

        protected void InsertDefaultEmailTemplates()
        {
            var list = new List<EmailTemplate>();

            foreach (var templateToBuild in BuiltInEmailTemplate.Templates)
            {
                var template = new EmailTemplate
                {
                    Id = Guid.NewGuid(),
                    Content = templateToBuild.DefaultContent,
                    Subject = templateToBuild.DefaultSubject,
                    DeveloperName = templateToBuild.DeveloperName,
                    IsBuiltInTemplate = true,
                };
                list.Add(template);
            }

            _db.EmailTemplates.AddRange(list);
        }

        protected void InsertDefaultAuthentications()
        {
            var list = new List<AuthenticationScheme>
            {
                new AuthenticationScheme
                {
                    Label = "Email address and password",
                    DeveloperName = AuthenticationSchemeType.EmailAndPassword,
                    IsBuiltInAuth = true,
                    IsEnabledForAdmins = true,
                    IsEnabledForUsers = true,
                    AuthenticationSchemeType = AuthenticationSchemeType.EmailAndPassword,
                    LoginButtonText = "Login with your email and password",
                    BruteForceProtectionMaxFailedAttempts = 10,
                    BruteForceProtectionWindowInSeconds = 60,
                },
                new AuthenticationScheme
                {
                    Label = "Magic link",
                    DeveloperName = AuthenticationSchemeType.MagicLink,
                    IsBuiltInAuth = true,
                    IsEnabledForAdmins = false,
                    IsEnabledForUsers = false,
                    AuthenticationSchemeType = AuthenticationSchemeType.MagicLink,
                    LoginButtonText = "Email me a login link",
                    MagicLinkExpiresInSeconds = 900,
                },
            };
            _db.AuthenticationSchemes.AddRange(list);
        }

        protected void InsertDefaultNavigationMenu()
        {
            var mainMenuId = Guid.NewGuid();
            var mainMenu = new NavigationMenu
            {
                Id = mainMenuId,
                Label = "Main menu",
                DeveloperName = "mainmenu",
                IsMainMenu = true,
                NavigationMenuItems = new List<NavigationMenuItem>
                {
                    new NavigationMenuItem
                    {
                        Id = Guid.NewGuid(),
                        Label = "Home",
                        Url = "/home",
                        IsDisabled = false,
                        OpenInNewTab = false,
                        CssClassName = "nav-link",
                        Ordinal = 1,
                        NavigationMenuId = mainMenuId,
                    },
                    new NavigationMenuItem
                    {
                        Id = Guid.NewGuid(),
                        Label = "About",
                        Url = "/about",
                        IsDisabled = false,
                        OpenInNewTab = false,
                        CssClassName = "nav-link",
                        Ordinal = 2,
                        NavigationMenuId = mainMenuId,
                    },
                    new NavigationMenuItem
                    {
                        Id = Guid.NewGuid(),
                        Label = "Posts",
                        Url = "/posts",
                        IsDisabled = false,
                        OpenInNewTab = false,
                        CssClassName = "nav-link",
                        Ordinal = 3,
                        NavigationMenuId = mainMenuId,
                    },
                },
            };
            _db.NavigationMenus.Add(mainMenu);
        }

        protected void InsertDefaultSitePages(Guid defaultThemeId)
        {
            // Get the Home template ID
            var homeTemplateId = _db
                .WebTemplates.Where(wt =>
                    wt.DeveloperName == BuiltInWebTemplate.HomePage.DeveloperName
                    && wt.ThemeId == defaultThemeId
                )
                .Select(wt => wt.Id)
                .First();

            // Get the Sidebar template ID for About page
            var sidebarTemplateId = _db
                .WebTemplates.Where(wt =>
                    wt.DeveloperName == BuiltInWebTemplate.PageSidebar.DeveloperName
                    && wt.ThemeId == defaultThemeId
                )
                .Select(wt => wt.Id)
                .First();

            // ==================== HOME PAGE ====================
            var homeWidgets = new Dictionary<string, List<SitePageWidget>>
            {
                ["hero"] = new List<SitePageWidget>
                {
                    new SitePageWidget
                    {
                        Id = Guid.NewGuid(),
                        WidgetType = BuiltInWidgetType.Hero.DeveloperName,
                        SettingsJson = System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                headline = "Welcome to Raytha",
                                subheadline = "You've successfully installed your new content management system. Head to the Admin Panel to start building something amazing.",
                                backgroundColor = "#0d6efd",
                                textColor = "#ffffff",
                                buttonText = "Admin Panel",
                                buttonUrl = "/raytha",
                                buttonStyle = "light",
                                alignment = "center",
                                minHeight = 450,
                            }
                        ),
                        Row = 0,
                        Column = 0,
                        ColumnSpan = 12,
                    },
                },
                ["features"] = new List<SitePageWidget>
                {
                    new SitePageWidget
                    {
                        Id = Guid.NewGuid(),
                        WidgetType = BuiltInWidgetType.Wysiwyg.DeveloperName,
                        SettingsJson = System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                content = @"<div class=""text-center mb-5"">
<span class=""badge bg-primary bg-opacity-10 text-primary px-3 py-2 rounded-pill mb-3"">
  <i class=""bi bi-stars me-1""></i>Features
</span>
<h2 class=""display-6 fw-bold text-dark"">Everything you need</h2>
<p class=""lead text-secondary col-lg-8 mx-auto"">Raytha gives you complete control over your content and templates with powerful, developer-friendly features.</p>
</div>
<div class=""row g-4"">
  <div class=""col-md-6 col-lg-3"">
    <div class=""card h-100 border-0 shadow-sm"">
      <div class=""card-body text-center p-4"">
        <div class=""bg-primary bg-opacity-10 text-primary rounded-circle d-inline-flex align-items-center justify-content-center p-3 mb-3"" style=""width: 64px; height: 64px;"">
          <i class=""bi bi-collection fs-2""></i>
        </div>
        <h5 class=""card-title fw-semibold"">Custom Content Types</h5>
        <p class=""card-text text-secondary small"">Define exactly how your content is structured with flexible custom fields.</p>
      </div>
    </div>
  </div>
  <div class=""col-md-6 col-lg-3"">
    <div class=""card h-100 border-0 shadow-sm"">
      <div class=""card-body text-center p-4"">
        <div class=""bg-success bg-opacity-10 text-success rounded-circle d-inline-flex align-items-center justify-content-center p-3 mb-3"" style=""width: 64px; height: 64px;"">
          <i class=""bi bi-code-slash fs-2""></i>
        </div>
        <h5 class=""card-title fw-semibold"">Liquid Templates</h5>
        <p class=""card-text text-secondary small"">Build dynamic pages with the powerful Liquid templating engine.</p>
      </div>
    </div>
  </div>
  <div class=""col-md-6 col-lg-3"">
    <div class=""card h-100 border-0 shadow-sm"">
      <div class=""card-body text-center p-4"">
        <div class=""bg-info bg-opacity-10 text-info rounded-circle d-inline-flex align-items-center justify-content-center p-3 mb-3"" style=""width: 64px; height: 64px;"">
          <i class=""bi bi-shield-check fs-2""></i>
        </div>
        <h5 class=""card-title fw-semibold"">User Management</h5>
        <p class=""card-text text-secondary small"">Built-in authentication and role-based access control.</p>
      </div>
    </div>
  </div>
  <div class=""col-md-6 col-lg-3"">
    <div class=""card h-100 border-0 shadow-sm"">
      <div class=""card-body text-center p-4"">
        <div class=""bg-warning bg-opacity-10 text-warning rounded-circle d-inline-flex align-items-center justify-content-center p-3 mb-3"" style=""width: 64px; height: 64px;"">
          <i class=""bi bi-lightning-charge fs-2""></i>
        </div>
        <h5 class=""card-title fw-semibold"">Fast & Flexible</h5>
        <p class=""card-text text-secondary small"">Built with .NET for performance and adaptability.</p>
      </div>
    </div>
  </div>
</div>",
                                padding = "medium",
                            }
                        ),
                        Row = 0,
                        Column = 0,
                        ColumnSpan = 12,
                    },
                },
                ["content"] = new List<SitePageWidget>
                {
                    new SitePageWidget
                    {
                        Id = Guid.NewGuid(),
                        WidgetType = BuiltInWidgetType.ContentList.DeveloperName,
                        SettingsJson = System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                headline = "Latest Posts",
                                subheadline = "Check out our recent articles and updates",
                                contentType = POSTS_DEVELOPER_NAME,
                                pageSize = 3,
                                displayStyle = "cards",
                                showImage = true,
                                showDate = true,
                                showExcerpt = true,
                                linkText = "View all posts",
                                linkUrl = "/posts",
                            }
                        ),
                        Row = 0,
                        Column = 0,
                        ColumnSpan = 12,
                    },
                },
                ["cta"] = new List<SitePageWidget>
                {
                    new SitePageWidget
                    {
                        Id = Guid.NewGuid(),
                        WidgetType = BuiltInWidgetType.CTA.DeveloperName,
                        SettingsJson = System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                headline = "Ready to get started?",
                                content = "Explore the Admin Panel to create content types, add content, and customize your templates.",
                                buttonText = "Launch Admin Panel",
                                buttonUrl = "/raytha",
                                buttonStyle = "light",
                                backgroundColor = "#212529",
                                textColor = "#ffffff",
                                alignment = "center",
                            }
                        ),
                        Row = 0,
                        Column = 0,
                        ColumnSpan = 12,
                    },
                },
            };

            var homePage = new SitePage
            {
                Id = homeSitePageGuid,
                Title = "Home",
                IsPublished = true,
                IsDraft = false,
                WebTemplateId = homeTemplateId,
                Route = new Route { SitePageId = homeSitePageGuid, Path = "home" },
                PublishedWidgets = homeWidgets,
                DraftWidgets = homeWidgets,
            };
            _db.SitePages.Add(homePage);

            // ==================== ABOUT PAGE ====================
            var aboutHtml =
                @"<div class=""mb-4 text-muted small""><p>The following content is default typography elements for your convenience while you style your new website.</p></div><h1 class=""mb-3"">h1. heading</h1><h2 class=""mb-3"">h2. heading</h2><h3 class=""mb-3"">h3. heading</h3><h4 class=""mb-2"">h4. heading</h4><h5 class=""mb-2"">h5. heading</h5><h6 class=""mb-4 text-uppercase text-muted"">h6. heading</h6><p class=""lead"">This is a normal paragraph of text with some <strong>bold</strong>, <em>italic</em>, <u>underlined</u>, <mark>highlighted</mark>, small print, <code>inline code</code>, H<sub>2</sub>O, and E = mc<sup>2</sup>.</p><p>Here is a link to <a href=""https://raytha.com"" target=""_blank"" rel=""noopener"" class=""link-primary"">raytha.com</a> inside a paragraph.</p><div class=""mb-4""><p><strong>Lorem ipsum dolor sit amet</strong>, <em>consectetur adipiscing elit. Aenean facilisis</em>, <s>nisl ac efficitur viverra, augue mauris rutrum magna</s>, ut placerat nisi odio id quam. Pellentesque quis augue sed lorem sollicitudin faucibus vel ut eros. Suspendisse vitae libero urna. Sed sit amet nunc condimentum, consequat leo imperdiet, sollicitudin erat. Vestibulum ex odio, vulputate eget lacus quis, mollis volutpat tortor.</p></div><figure class=""mb-4""><blockquote class=""blockquote""><p>Mauris leo magna, rutrum sit amet nunc at, sollicitudin venenatis elit. Nam id purus vel purus fermentum semper quis porta purus. Ut malesuada, dolor sed condimentum lacinia, nunc sapien tristique lorem, ac consequat sapien augue a sem.</p></blockquote><figcaption class=""blockquote-footer"">Example blockquote caption</figcaption></figure><hr class=""my-4""><ul class=""list-group mb-4""><li class=""list-group-item""><p>Unordered list item 1</p></li><li class=""list-group-item""><p>Unordered list item 2</p></li><li class=""list-group-item""><p>Unordered list item 3</p></li></ul><ol class=""list-group list-group-numbered mb-4""><li class=""list-group-item""><p>Ordered list item 1</p></li><li class=""list-group-item""><p>Ordered list item 2</p></li><li class=""list-group-item""><p>Ordered list item 3</p></li></ol><pre class=""bg-light p-3 rounded mb-3""><code>{
  ""example"": ""code"",
  ""anotherKey"": 123
}</code></pre><table class=""table table-bordered table-striped table-hover mb-4""><tbody><tr><th colspan=""1"" rowspan=""1""><p>Column A</p></th><th colspan=""1"" rowspan=""1""><p>Column B</p></th><th colspan=""1"" rowspan=""1""><p>Column C</p></th></tr><tr><td colspan=""1"" rowspan=""1""><p>Row 1, A</p></td><td colspan=""1"" rowspan=""1""><p>Row 1, B</p></td><td colspan=""1"" rowspan=""1""><p>Row 1, C</p></td></tr><tr><td colspan=""1"" rowspan=""1""><p>Row 2, A</p></td><td colspan=""1"" rowspan=""1""><p>Row 2, B</p></td><td colspan=""1"" rowspan=""1""><p>Row 2, C</p></td></tr></tbody></table><p class=""mb-4""><button class=""btn btn-primary me-2"" type=""button"">Primary Button</button> <button class=""btn btn-secondary me-2"" type=""button"">Secondary Button</button> <a href=""#"" class=""btn btn-outline-primary"">Link Button</a></p><div class=""alert alert-warning mb-4"" role=""alert""><p>This is a Bootstrap warning alert for testing.</p></div>";

            var aboutWidgets = new Dictionary<string, List<SitePageWidget>>
            {
                ["main"] = new List<SitePageWidget>
                {
                    new SitePageWidget
                    {
                        Id = Guid.NewGuid(),
                        WidgetType = BuiltInWidgetType.Wysiwyg.DeveloperName,
                        SettingsJson = System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                content = @"<h1 class=""display-5 fw-bold mb-4"">About Us</h1>
<p class=""lead"">Welcome to our site! This is a sample About page demonstrating the Site Pages feature with a two-column layout.</p>
<hr class=""my-4"">" + aboutHtml,
                                padding = "medium",
                            }
                        ),
                        Row = 0,
                        Column = 0,
                        ColumnSpan = 12,
                    },
                },
                ["sidebar"] = new List<SitePageWidget>
                {
                    new SitePageWidget
                    {
                        Id = Guid.NewGuid(),
                        WidgetType = BuiltInWidgetType.Card.DeveloperName,
                        SettingsJson = System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                title = "Get Started",
                                description = "Ready to explore? Head to the Admin Panel to customize your site.",
                                buttonText = "Admin Panel",
                                buttonUrl = "/raytha",
                                buttonStyle = "primary",
                            }
                        ),
                        Row = 0,
                        Column = 0,
                        ColumnSpan = 12,
                    },
                    new SitePageWidget
                    {
                        Id = Guid.NewGuid(),
                        WidgetType = BuiltInWidgetType.ContentList.DeveloperName,
                        SettingsJson = System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                headline = "Recent Posts",
                                contentType = POSTS_DEVELOPER_NAME,
                                pageSize = 5,
                                displayStyle = "compact",
                                showDate = true,
                                showExcerpt = false,
                            }
                        ),
                        Row = 1,
                        Column = 0,
                        ColumnSpan = 12,
                    },
                },
            };

            var aboutPage = new SitePage
            {
                Id = aboutSitePageGuid,
                Title = "About",
                IsPublished = true,
                IsDraft = false,
                WebTemplateId = sidebarTemplateId,
                Route = new Route { SitePageId = aboutSitePageGuid, Path = "about" },
                PublishedWidgets = aboutWidgets,
                DraftWidgets = aboutWidgets,
            };
            _db.SitePages.Add(aboutPage);
        }

        protected void InsertDefaultPosts()
        {
            var postTemplateId = _db
                .WebTemplates.Where(wt =>
                    wt.DeveloperName == BuiltInWebTemplate.ContentItemDetailViewPage.DeveloperName
                )
                .Select(wt => wt.Id)
                .First();

            // Post 1: Hello World!
            dynamic post1Content = new ExpandoObject();
            post1Content.title = "Hello World!";
            post1Content.content =
                @"
<div><!--block-->If you're reading this, it means you've successfully installed your CMS and created your first blog post. Congratulations!<br><br></div>
<div><!--block-->This is the ""Hello World"" of the blogging world - the first post that many bloggers create to test out their new platform. Now that everything is up and running, it's time to start creating and sharing your content with the world.<br><br></div>
<div><!--block-->To get started, you might want to familiarize yourself with the features and tools of your CMS. Some things you might want to explore include:<br><br></div>
<ul><li><!--block-->Adding content, pages, and posts</li><li><!--block-->Customizing the look and feel of your blog by modifying the templates</li><li><!--block-->Setting up user accounts and permissions for other contributors</li></ul>
<div><!--block--><br></div>
<div><!--block-->As you start to use your CMS and create more posts, don't be afraid to experiment and try out new things. The best way to learn is by doing, so have fun and see what you can create!</div>
";
            var post1Id = Guid.NewGuid();
            var post1 = new ContentItem
            {
                Id = post1Id,
                DraftContent = post1Content,
                PublishedContent = post1Content,
                IsPublished = true,
                IsDraft = false,
                ContentTypeId = postTypeGuid,
                Route = new Route
                {
                    ContentItemId = post1Id,
                    Path = $"{((string)post1Content.title).ToUrlSlug()}",
                },
            };
            _db.ContentItems.Add(post1);
            _db.WebTemplateContentItemRelations.Add(
                new WebTemplateContentItemRelation
                {
                    Id = Guid.NewGuid(),
                    WebTemplateId = postTemplateId,
                    ContentItemId = post1Id,
                }
            );

            // Post 2: Getting Started with Raytha
            dynamic post2Content = new ExpandoObject();
            post2Content.title = "Getting Started with Raytha";
            post2Content.content =
                @"
<div><!--block-->Welcome to Raytha! This guide will help you get up and running quickly with your new content management system.<br><br></div>
<h3><!--block-->Key Concepts</h3>
<div><!--block-->Raytha is built around a few core concepts:<br><br></div>
<ul>
<li><!--block--><strong>Content Types</strong> - Define the structure of your content (like Posts, Products, or Events)</li>
<li><!--block--><strong>Views</strong> - Create different ways to display your content lists</li>
<li><!--block--><strong>Templates</strong> - Control the look and feel using Liquid templates</li>
<li><!--block--><strong>Site Pages</strong> - Build standalone pages with drag-and-drop widgets</li>
</ul>
<div><!--block--><br></div>
<h3><!--block-->Next Steps</h3>
<div><!--block-->Explore the admin panel to discover all the features available to you. Start by creating a new content type or customizing the default templates to match your brand.</div>
";
            var post2Id = Guid.NewGuid();
            var post2 = new ContentItem
            {
                Id = post2Id,
                DraftContent = post2Content,
                PublishedContent = post2Content,
                IsPublished = true,
                IsDraft = false,
                ContentTypeId = postTypeGuid,
                Route = new Route
                {
                    ContentItemId = post2Id,
                    Path = $"{((string)post2Content.title).ToUrlSlug()}",
                },
            };
            _db.ContentItems.Add(post2);
            _db.WebTemplateContentItemRelations.Add(
                new WebTemplateContentItemRelation
                {
                    Id = Guid.NewGuid(),
                    WebTemplateId = postTemplateId,
                    ContentItemId = post2Id,
                }
            );

            // Post 3: Customizing Your Templates
            dynamic post3Content = new ExpandoObject();
            post3Content.title = "Customizing Your Templates";
            post3Content.content =
                @"
<div><!--block-->Raytha uses the Liquid templating language, making it easy to customize how your content is displayed.<br><br></div>
<h3><!--block-->Template Basics</h3>
<div><!--block-->Templates in Raytha consist of:<br><br></div>
<ul>
<li><!--block--><strong>Base Layouts</strong> - The overall page structure (header, footer, navigation)</li>
<li><!--block--><strong>Page Templates</strong> - Templates for content item detail views and list views</li>
<li><!--block--><strong>Widget Templates</strong> - Templates for individual widgets on Site Pages</li>
</ul>
<div><!--block--><br></div>
<h3><!--block-->Getting Started with Customization</h3>
<div><!--block-->Navigate to <strong>Design &gt; Templates</strong> in the admin panel to view and edit your templates. You can use Liquid tags like <code>{{ Target.Title }}</code> to output content fields, and <code>{% if condition %}</code> for conditional logic.<br><br></div>
<div><!--block-->For more advanced customization, check out the <a href=""https://shopify.github.io/liquid/"" target=""_blank"">Liquid documentation</a>.</div>
";
            var post3Id = Guid.NewGuid();
            var post3 = new ContentItem
            {
                Id = post3Id,
                DraftContent = post3Content,
                PublishedContent = post3Content,
                IsPublished = true,
                IsDraft = false,
                ContentTypeId = postTypeGuid,
                Route = new Route
                {
                    ContentItemId = post3Id,
                    Path = $"{((string)post3Content.title).ToUrlSlug()}",
                },
            };
            _db.ContentItems.Add(post3);
            _db.WebTemplateContentItemRelations.Add(
                new WebTemplateContentItemRelation
                {
                    Id = Guid.NewGuid(),
                    WebTemplateId = postTemplateId,
                    ContentItemId = post3Id,
                }
            );

            // Post 4: Managing Content Types
            dynamic post4Content = new ExpandoObject();
            post4Content.title = "Managing Content Types";
            post4Content.content =
                @"
<div><!--block-->Content Types are the building blocks of your Raytha site. They define what kind of content you can create and manage.<br><br></div>
<h3><!--block-->What is a Content Type?</h3>
<div><!--block-->A Content Type is like a blueprint for your content. For example, a ""Blog Post"" content type might have fields for Title, Content, Featured Image, and Author. An ""Event"" content type might have Date, Location, and Description fields.<br><br></div>
<h3><!--block-->Creating a Content Type</h3>
<div><!--block-->To create a new Content Type:<br><br></div>
<ol>
<li><!--block-->Go to <strong>Content</strong> in the admin panel</li>
<li><!--block-->Click <strong>Create Content Type</strong></li>
<li><!--block-->Give it a name and developer name</li>
<li><!--block-->Add the fields you need</li>
</ol>
<div><!--block--><br></div>
<h3><!--block-->Field Types</h3>
<div><!--block-->Raytha supports many field types including Single Line Text, Long Text, WYSIWYG Editor, Number, Date, Checkbox, Dropdown, Attachment, and more. Choose the right field type based on the kind of data you want to store.</div>
";
            var post4Id = Guid.NewGuid();
            var post4 = new ContentItem
            {
                Id = post4Id,
                DraftContent = post4Content,
                PublishedContent = post4Content,
                IsPublished = true,
                IsDraft = false,
                ContentTypeId = postTypeGuid,
                Route = new Route
                {
                    ContentItemId = post4Id,
                    Path = $"{((string)post4Content.title).ToUrlSlug()}",
                },
            };
            _db.ContentItems.Add(post4);
            _db.WebTemplateContentItemRelations.Add(
                new WebTemplateContentItemRelation
                {
                    Id = Guid.NewGuid(),
                    WebTemplateId = postTemplateId,
                    ContentItemId = post4Id,
                }
            );
        }

        protected void InsertDefaultViews()
        {
            var listViewTemplateId = _db
                .WebTemplates.Where(wt =>
                    wt.DeveloperName == BuiltInWebTemplate.ContentItemListViewPage.DeveloperName
                )
                .Select(wt => wt.Id)
                .First();

            var defaultPostsViewId = Guid.NewGuid();
            var defaultPostsView = new View
            {
                Id = defaultPostsViewId,
                Label = $"All {POSTS_NAME_PLURAL.ToLower()}",
                DeveloperName = POSTS_DEVELOPER_NAME,
                ContentTypeId = postTypeGuid,
                Columns = new[]
                {
                    BuiltInContentTypeField.PrimaryField.DeveloperName,
                    BuiltInContentTypeField.CreationTime.DeveloperName,
                    BuiltInContentTypeField.Template,
                },
                Route = new Route { Path = POSTS_DEVELOPER_NAME, ViewId = defaultPostsViewId },
                IsPublished = true,
            };
            _db.Views.Add(defaultPostsView);

            var postsWebTemplateViewRelation = new WebTemplateViewRelation
            {
                Id = Guid.NewGuid(),
                WebTemplateId = listViewTemplateId,
                ViewId = defaultPostsViewId,
            };

            _db.WebTemplateViewRelations.Add(postsWebTemplateViewRelation);
        }

        protected void SetPrimaryFieldsOnContentTypes()
        {
            var contentTypes = _db.ContentTypes.Where(p => true);
            foreach (var contentType in contentTypes)
            {
                contentType.PrimaryFieldId = postsTitleFieldGuid;
            }
        }

        protected void SetHomePage()
        {
            var orgSettings = _db.OrganizationSettings.First();
            orgSettings.HomePageId = homeSitePageGuid;
            orgSettings.HomePageType = Route.SITE_PAGE_TYPE;
        }
    }
}
