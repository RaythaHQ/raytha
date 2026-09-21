using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.Commands;

public class EnsureDefaultThemeContent
{
    public record Command : IRequest<CommandResponseDto<ShortGuid>> { }

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
            var theme = await ResolveActiveTheme(cancellationToken);
            if (theme is null)
            {
                return new CommandResponseDto<ShortGuid>(ShortGuid.Empty);
            }

            var hasWebTemplates = await _db.WebTemplates.AnyAsync(
                wt => wt.ThemeId == theme.Id,
                cancellationToken
            );
            if (!hasWebTemplates)
            {
                var mediaItems = await _db
                    .ThemeAccessToMediaItems.Where(t => t.ThemeId == theme.Id)
                    .Select(t => t.MediaItem!)
                    .ToListAsync(cancellationToken);

                InsertDefaultWebTemplates(mediaItems, theme.Id);
                InsertDefaultWidgetTemplates(theme.Id);
                await _db.SaveChangesAsync(cancellationToken);
            }

            await AssignBuiltInTemplatesToHomeAndAbout(theme.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(theme.Id);
        }

        private async Task<Theme?> ResolveActiveTheme(CancellationToken cancellationToken)
        {
            var orgSettings = await _db.OrganizationSettings.FirstOrDefaultAsync(cancellationToken);
            if (orgSettings is not null && orgSettings.ActiveThemeId != Guid.Empty)
            {
                var active = await _db.Themes.FirstOrDefaultAsync(
                    t => t.Id == orgSettings.ActiveThemeId,
                    cancellationToken
                );
                if (active is not null)
                {
                    return active;
                }
            }

            return await _db.Themes.OrderBy(t => t.Id).FirstOrDefaultAsync(cancellationToken);
        }

        private async Task AssignBuiltInTemplatesToHomeAndAbout(
            Guid themeId,
            CancellationToken cancellationToken
        )
        {
            var homeTemplateId = await _db
                .WebTemplates.Where(wt =>
                    wt.ThemeId == themeId
                    && wt.DeveloperName == BuiltInWebTemplate.HomePage.DeveloperName
                )
                .Select(wt => wt.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var sidebarTemplateId = await _db
                .WebTemplates.Where(wt =>
                    wt.ThemeId == themeId
                    && wt.DeveloperName == BuiltInWebTemplate.PageSidebar.DeveloperName
                )
                .Select(wt => wt.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (homeTemplateId != Guid.Empty)
            {
                var home = await _db
                    .SitePages.Where(p => p.Route != null && p.Route.Path == "home")
                    .FirstOrDefaultAsync(cancellationToken);
                if (home is not null && home.WebTemplateId == Guid.Empty)
                {
                    home.WebTemplateId = homeTemplateId;
                }
            }

            if (sidebarTemplateId != Guid.Empty)
            {
                var about = await _db
                    .SitePages.Where(p => p.Route != null && p.Route.Path == "about")
                    .FirstOrDefaultAsync(cancellationToken);
                if (about is not null && about.WebTemplateId == Guid.Empty)
                {
                    about.WebTemplateId = sidebarTemplateId;
                }
            }
        }

        private void InsertDefaultWebTemplates(
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
                var mediaItem = mediaItems.FirstOrDefault(mi => mi.FileName.Contains(fileName));
                if (mediaItem is not null)
                {
                    updatedContent = updatedContent.Replace(fileName, mediaItem.ObjectKey);
                }
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

            var postTypeId = _db
                .ContentTypes.Where(ct => ct.DeveloperName == "posts")
                .Select(ct => ct.Id)
                .FirstOrDefault();

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

                var templateAccess = new List<WebTemplateAccessToModelDefinition>();
                if (postTypeId != Guid.Empty)
                {
                    templateAccess.Add(
                        new WebTemplateAccessToModelDefinition { ContentTypeId = postTypeId }
                    );
                }

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

        private void InsertDefaultWidgetTemplates(Guid defaultThemeId)
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
    }
}
