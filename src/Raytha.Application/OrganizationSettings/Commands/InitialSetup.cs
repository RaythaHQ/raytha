using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;
using System.Dynamic;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

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
            RuleFor(x => x.SmtpHost).NotEmpty().When(p => emailerConfiguration.IsMissingSmtpEnvVars());
            RuleFor(x => x.SmtpPort).NotNull().GreaterThan(0).LessThanOrEqualTo(65535).When(p => emailerConfiguration.IsMissingSmtpEnvVars());
            RuleFor(x => x.OrganizationName).NotEmpty();
            RuleFor(x => x.TimeZone).Must(DateTimeExtensions.IsValidTimeZone)
                .WithMessage(p => $"{p.TimeZone} timezone is unrecognized.");
            RuleFor(x => x.WebsiteUrl).Must(StringExtensions.IsValidUriFormat)
                .WithMessage(p => $"{p.WebsiteUrl} must be a valid URI format.");
            RuleFor(x => x.SmtpDefaultFromAddress).EmailAddress();
            RuleFor(x => x.SmtpDefaultFromName).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private const string PAGES_NAME_PLURAL = "Pages";
        private const string PAGES_NAME_SINGULAR = "Page";
        private const string PAGES_DEVELOPER_NAME = "pages";

        private const string POSTS_NAME_PLURAL = "Posts";
        private const string POSTS_NAME_SINGULAR = "Post";
        private const string POSTS_DEVELOPER_NAME = "posts";

        private const string TITLE_FIELD_LABEL = "Title";
        private const string TITLE_FIELD_DEVELOPER_NAME = "title";
        private const string CONTENT_FIELD_LABEL = "Content";
        private const string CONTENT_FIELD_DEVELOPER_NAME = "content";

        private const string DEFAULT_THEME_DEVELOPER_NAME = "raytha_default_2024";

        Guid pageTypeGuid = Guid.NewGuid();
        Guid postTypeGuid = Guid.NewGuid();

        Guid pageTitleFieldGuid = Guid.NewGuid();
        Guid postsTitleFieldGuid = Guid.NewGuid();

        Guid homePageGuid = Guid.NewGuid();
        Guid orgSettingsGuid = Guid.NewGuid();

        private readonly IRaythaDbContext _db;
        private readonly IEmailerConfiguration _emailerConfiguration;
        private readonly IFileStorageProvider _fileStorageProvider;

        public Handler(IRaythaDbContext db, IEmailerConfiguration emailerConfiguration, IFileStorageProvider fileStorageProvider)
        {
            _db = db;
            _emailerConfiguration = emailerConfiguration;
            _fileStorageProvider = fileStorageProvider;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var defaultThemeId = await _db.Themes
                .Select(t => t.Id)
                .FirstAsync(cancellationToken);

            InsertOrganizationSettings(request, defaultThemeId);
            InsertDefaultContentTypes();
            InsertDefaultContentTypeFields();
            InsertDefaultRolesAndSuperAdmin(request);

            var mediaItemObjectKeys = await InsertDefaultThemeAssetsToFileStorage(cancellationToken, defaultThemeId);
            InsertDefaultWebTemplates(mediaItemObjectKeys, defaultThemeId);
            InsertDefaultEmailTemplates();
            InsertDefaultAuthentications();
            await _db.SaveChangesAsync(cancellationToken);

            InsertDefaultPages();
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
                    new ContentTypeRolePermission { ContentTypeId = pageTypeGuid, ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum },
                    new ContentTypeRolePermission { ContentTypeId = postTypeGuid, ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum }
                },
                CreationTime = DateTime.UtcNow
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
                    new ContentTypeRolePermission { ContentTypeId = pageTypeGuid, ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum },
                    new ContentTypeRolePermission { ContentTypeId = postTypeGuid, ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum }
                },
                CreationTime = DateTime.UtcNow
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
                    new ContentTypeRolePermission { ContentTypeId = pageTypeGuid, ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum },
                    new ContentTypeRolePermission { ContentTypeId = postTypeGuid, ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum }
                },
                CreationTime = DateTime.UtcNow
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
                IsAdmin = true
            };
            _db.Users.Add(superAdmin);
        }

        protected void InsertDefaultContentTypeFields()
        {
            var titlePageField = new ContentTypeField
            {
                Id = pageTitleFieldGuid,
                Label = TITLE_FIELD_LABEL,
                DeveloperName = TITLE_FIELD_DEVELOPER_NAME,
                ContentTypeId = pageTypeGuid,
                FieldOrder = 1,
                FieldType = BaseFieldType.SingleLineText
            };
            _db.ContentTypeFields.Add(titlePageField);

            var contentPageField = new ContentTypeField
            {
                Id = Guid.NewGuid(),
                Label = CONTENT_FIELD_LABEL,
                DeveloperName = CONTENT_FIELD_DEVELOPER_NAME,
                ContentTypeId = pageTypeGuid,
                FieldOrder = 2,
                FieldType = BaseFieldType.Wysiwyg
            };
            _db.ContentTypeFields.Add(contentPageField);

            var titlePostField = new ContentTypeField
            {
                Id = postsTitleFieldGuid,
                Label = TITLE_FIELD_LABEL,
                DeveloperName = TITLE_FIELD_DEVELOPER_NAME,
                ContentTypeId = postTypeGuid,
                FieldOrder = 1,
                FieldType = BaseFieldType.SingleLineText
            };
            _db.ContentTypeFields.Add(titlePostField);

            var contentPostField = new ContentTypeField
            {
                Id = Guid.NewGuid(),
                Label = CONTENT_FIELD_LABEL,
                DeveloperName = CONTENT_FIELD_DEVELOPER_NAME,
                ContentTypeId = postTypeGuid,
                FieldOrder = 2,
                FieldType = BaseFieldType.Wysiwyg
            };
            _db.ContentTypeFields.Add(contentPostField);
        }

        protected void InsertDefaultContentTypes()
        {
            var pageContentType = new ContentType
            {
                Id = pageTypeGuid,
                LabelPlural = PAGES_NAME_PLURAL,
                LabelSingular = PAGES_NAME_SINGULAR,
                DeveloperName = PAGES_DEVELOPER_NAME,
                IsActive = true,
                IsDeleted = false,
                DefaultRouteTemplate = "{PrimaryField}"
            };
            _db.ContentTypes.Add(pageContentType);

            var postContentType = new ContentType
            {
                Id = postTypeGuid,
                LabelPlural = POSTS_NAME_PLURAL,
                LabelSingular = POSTS_NAME_SINGULAR,
                DeveloperName = POSTS_DEVELOPER_NAME,
                IsActive = true,
                IsDeleted = false,
                DefaultRouteTemplate = "{CurrentYear}/{CurrentMonth}/{PrimaryField}"
            };
            _db.ContentTypes.Add(postContentType);
        }

        protected async Task<IReadOnlyCollection<MediaItem>> InsertDefaultThemeAssetsToFileStorage(CancellationToken cancellationToken, Guid defaultThemeId)
        {
            var mediaItems = new List<MediaItem>();
            var themeAccessToMediaItems = new List<ThemeAccessToMediaItem>();

            var defaultThemeAssetsPath = Path.Combine("wwwroot", DEFAULT_THEME_DEVELOPER_NAME, "assets");
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
                    Id = idForKey.Guid,
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
                    ThemeId = defaultThemeId,
                    MediaItemId = idForKey.Guid,
                });
            }

            _db.MediaItems.AddRange(mediaItems);
            _db.ThemeAccessToMediaItems.AddRange(themeAccessToMediaItems);

            return mediaItems;
        }

        protected void InsertDefaultWebTemplates(IReadOnlyCollection<MediaItem> mediaItems, Guid defaultThemeId)
        {
            var baseLayoutFileNames = new[]
            {
                "favicon.ico",
                "bootstrap.min.css",
                "bootstrap.bundle.min.js",
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
                DeveloperName = defaultBaseLayout.DeveloperName
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
                ParentTemplateId = baseLayout.Id
            };
            list.Add(baseLoginLayout);

            var homePageMediaItem = "raythadotcom_screenshot.webp";

            foreach (var templateToBuild in BuiltInWebTemplate.Templates.Where(p =>
                            p.DeveloperName != BuiltInWebTemplate._Layout.DeveloperName &&
                            p.DeveloperName != BuiltInWebTemplate._LoginLayout.DeveloperName))
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
                    DeveloperName = templateToBuild.DeveloperName
                };

                var templateAccess = new List<WebTemplateAccessToModelDefinition>
                {
                    new WebTemplateAccessToModelDefinition { ContentTypeId = pageTypeGuid },
                    new WebTemplateAccessToModelDefinition { ContentTypeId = postTypeGuid }
                };

                var standardTemplatesForContentTypes = new List<string>
                {
                    BuiltInWebTemplate.HomePage,
                    BuiltInWebTemplate.ContentItemDetailViewPage,
                    BuiltInWebTemplate.ContentItemListViewPage
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
                    BuiltInWebTemplate.ChangeProfilePage
                };

                if (standardTemplatesForContentTypes.Contains(templateToBuild))
                {
                    template.TemplateAccessToModelDefinitions = templateAccess;
                    template.IsBuiltInTemplate = false;
                    template.AllowAccessForNewContentTypes = true;

                    if (templateToBuild.DeveloperName == BuiltInWebTemplate.HomePage.DeveloperName)
                    {
                        template.Content = template.Content.Replace(homePageMediaItem, mediaItems.First(mi => mi.FileName.Contains(homePageMediaItem)).ObjectKey);
                    }
                }
                else if (loginTemplates.Contains(templateToBuild))
                {
                    template.ParentTemplateId = baseLoginLayout.Id;
                }

                list.Add(template);
            }

            _db.WebTemplates.AddRange(list);
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
                    IsBuiltInTemplate = true
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
                    MagicLinkExpiresInSeconds = 900
                }
            };
            _db.AuthenticationSchemes.AddRange(list);
        }

        protected void InsertDefaultPages()
        {
            dynamic homePageContent = new ExpandoObject();
            homePageContent.title = "Home";
            var homePagePath = $"{((string)homePageContent.title).ToUrlSlug()}";
            homePageContent.content = @"
<div><!--block-->Welcome to our website! We are currently in the process of building and designing our new online home. We apologize for any inconvenience this may cause and appreciate your patience as we work to bring you the best possible experience. In the meantime, please feel free to contact us with any questions or inquiries you may have. We are always happy to help. Thank you for visiting and please check back soon for updates on our progress.&nbsp;</div>";
            var homePage = new ContentItem
            {
                Id = homePageGuid,
                DraftContent = homePageContent,
                PublishedContent = homePageContent,
                IsPublished = true,
                IsDraft = false,
                ContentTypeId = pageTypeGuid,
                Route = new Route
                {
                    ContentItemId = homePageGuid,
                    Path = homePagePath
                }
            };

            _db.ContentItems.Add(homePage);

            var homePageTemplateId = _db.WebTemplates
                .Where(wt => wt.DeveloperName == BuiltInWebTemplate.HomePage.DeveloperName)
                .Select(wt => wt.Id)
                .First();

            var homePageWebTemplateContentItemRelation = new WebTemplateContentItemRelation
            {
                Id = Guid.NewGuid(),
                WebTemplateId = homePageTemplateId,
                ContentItemId = homePageGuid,
            };

            _db.WebTemplateContentItemRelations.Add(homePageWebTemplateContentItemRelation);

            dynamic aboutPageContent = new ExpandoObject();
            aboutPageContent.title = "About";
            var aboutPagePath = $"{((string)aboutPageContent.title).ToUrlSlug()}";
            aboutPageContent.content = @"
<div><!--block-->The following content is default typography elements for your convenience while you style your new website.<br><br></div>
<h1><!--block-->h1. heading</h1>
<h2><!--block-->h2. heading</h2>
<h3><!--block-->h3. heading</h3>
<h4><!--block-->h4. heading</h4>
<h5><!--block-->h5. heading</h5>
<h6><!--block-->h6. heading</h6>
<div><!--block--><br></div>
<div><!--block--><strong>Lorem ipsum dolor sit amet</strong>, <em>consectetur adipiscing elit. Aenean facilisis</em>, <del>nisl ac efficitur viverra, augue mauris rutrum magna</del>, ut placerat nisi odio id quam. Pellentesque quis augue sed lorem sollicitudin faucibus vel ut eros. Suspendisse vitae libero urna. Sed sit amet nunc condimentum, consequat leo imperdiet, sollicitudin erat. Vestibulum ex odio, vulputate eget lacus quis, mollis volutpat tortor. Vestibulum consectetur arcu nec urna placerat, in tempor purus porta. Pellentesque ligula risus, volutpat sit amet facilisis vitae, maximus ac enim. Duis arcu urna, sodales non ex vel, pharetra commodo lorem.<br><br><a href=""https://raytha.com"">raytha.com</a><br><br></div>
<blockquote><!--block-->Mauris leo magna, rutrum sit amet nunc at, sollicitudin venenatis elit. Nam id purus vel purus fermentum semper quis porta purus. Ut malesuada, dolor sed condimentum lacinia, nunc sapien tristique lorem, ac consequat sapien augue a sem. Vivamus ac venenatis massa. Phasellus in rhoncus nulla. Pellentesque tincidunt cursus urna, sed tincidunt nulla.</blockquote>
<div><!--block--><br></div>
<ul><li><!--block-->Unordered list item 1</li><li><!--block-->Unordered list item 2</li><li><!--block-->Unordered list item 3</li></ul>
<div><!--block--><br></div>
<ol><li><!--block-->Ordered list item 1</li><li><!--block-->Ordered list item 2</li><li><!--block-->Ordered list item 3</li></ol>
<div><!--block--><br></div>
<pre><!--block-->{""example"": ""code""}</pre>
<div><!--block--><br></div>
";
            var anotherPageId = Guid.NewGuid();
            var anotherPage = new ContentItem
            {
                Id = anotherPageId,
                DraftContent = aboutPageContent,
                PublishedContent = aboutPageContent,
                IsPublished = true,
                IsDraft = false,
                ContentTypeId = pageTypeGuid,
                Route = new Route
                {
                    ContentItemId = anotherPageId,
                    Path = aboutPagePath
                }
            };

            _db.ContentItems.Add(anotherPage);

            var aboutPageTemplateId = _db.WebTemplates
                .Where(wt => wt.DeveloperName == BuiltInWebTemplate.ContentItemDetailViewPage.DeveloperName)
                .Select(wt => wt.Id)
                .First();

            var aboutPageWebTemplateContentItemRelation = new WebTemplateContentItemRelation
            {
                Id = Guid.NewGuid(),
                WebTemplateId = aboutPageTemplateId,
                ContentItemId = anotherPageId,
            };

            _db.WebTemplateContentItemRelations.Add(aboutPageWebTemplateContentItemRelation);
        }

        protected void InsertDefaultPosts()
        {
            dynamic postContent = new ExpandoObject();
            postContent.title = "Hello World!";
            var homePagePath = $"{((string)postContent.title).ToUrlSlug()}";
            postContent.content = @"
<div><!--block-->If you're reading this, it means you've successfully installed your CMS and created your first blog post. Congratulations!<br><br></div>
<div><!--block-->This is the ""Hello World"" of the blogging world - the first post that many bloggers create to test out their new platform. Now that everything is up and running, it's time to start creating and sharing your content with the world.<br><br></div>
<div><!--block-->To get started, you might want to familiarize yourself with the features and tools of your CMS. Some things you might want to explore include:<br><br></div>
<ul><li><!--block-->Adding content, pages, and posts</li><li><!--block-->Customizing the look and feel of your blog by modifying the templates</li><li><!--block-->Setting up user accounts and permissions for other contributors</li></ul>
<div><!--block--><br></div>
<div><!--block-->As you start to use your CMS and create more posts, don't be afraid to experiment and try out new things. The best way to learn is by doing, so have fun and see what you can create!</div>
";
            var postId = Guid.NewGuid();
            var post = new ContentItem
            {
                Id = postId,
                DraftContent = postContent,
                PublishedContent = postContent,
                IsPublished = true,
                IsDraft = false,
                ContentTypeId = postTypeGuid,
                Route = new Route
                {
                    ContentItemId = postId,
                    Path = homePagePath
                }
            };

            _db.ContentItems.Add(post);

            var postTemplateId = _db.WebTemplates
                .Where(wt => wt.DeveloperName == BuiltInWebTemplate.ContentItemDetailViewPage.DeveloperName)
                .Select(wt => wt.Id)
                .First();

            var webTemplateContentItemRelation = new WebTemplateContentItemRelation
            {
                Id = Guid.NewGuid(),
                WebTemplateId = postTemplateId,
                ContentItemId = postId,
            };

            _db.WebTemplateContentItemRelations.Add(webTemplateContentItemRelation);
        }

        protected void InsertDefaultViews()
        {
            var defaultPageViewId = Guid.NewGuid();
            var defaultPageView = new View
            {
                Id = defaultPageViewId,
                Label = $"All {PAGES_NAME_PLURAL.ToLower()}",
                DeveloperName = PAGES_DEVELOPER_NAME,
                ContentTypeId = pageTypeGuid,
                Columns = new[] { BuiltInContentTypeField.PrimaryField.DeveloperName, BuiltInContentTypeField.CreationTime.DeveloperName, BuiltInContentTypeField.Template },
                Route = new Route
                {
                    Path = PAGES_DEVELOPER_NAME,
                    ViewId = defaultPageViewId
                },
                IsPublished = true
            };

            _db.Views.Add(defaultPageView);

            var listViewTemplateId = _db.WebTemplates
                .Where(wt => wt.DeveloperName == BuiltInWebTemplate.ContentItemListViewPage.DeveloperName)
                .Select(wt => wt.Id)
                .First();

            var pagesWebTemplateViewRelation = new WebTemplateViewRelation
            {
                Id = Guid.NewGuid(),
                WebTemplateId = listViewTemplateId,
                ViewId = defaultPageViewId,
            };

            _db.WebTemplateViewRelations.Add(pagesWebTemplateViewRelation);

            var defaultPostsViewId = Guid.NewGuid();
            var defaultPostsView = new View
            {
                Id = defaultPostsViewId,
                Label = $"All {POSTS_NAME_PLURAL.ToLower()}",
                DeveloperName = POSTS_DEVELOPER_NAME,
                ContentTypeId = postTypeGuid,
                Columns = new[] { BuiltInContentTypeField.PrimaryField.DeveloperName, BuiltInContentTypeField.CreationTime.DeveloperName, BuiltInContentTypeField.Template },
                Route = new Route
                {
                    Path = POSTS_DEVELOPER_NAME,
                    ViewId = defaultPostsViewId
                },
                IsPublished = true
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
                contentType.PrimaryFieldId = contentType.DeveloperName == PAGES_DEVELOPER_NAME ? pageTitleFieldGuid : postsTitleFieldGuid;
            }
        }

        protected void SetHomePage()
        {
            var orgSettings = _db.OrganizationSettings.First();
            orgSettings.HomePageId = homePageGuid;
        }
    }
}
