using System;
using System.Threading.Tasks;
using CSharpVitamins;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.MediaItems;
using Raytha.Application.MediaItems.Queries;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Application.NavigationMenus;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.Routes;
using Raytha.Application.Routes.Queries;
using Raytha.Application.SitePages;
using Raytha.Application.SitePages.Commands;
using Raytha.Application.SitePages.Queries;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.UserGroups;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;

namespace Raytha.Infrastructure.RaythaFunctions;

public class RaythaFunctionApi_V1 : IRaythaFunctionApi_V1
{
    private ISender Mediator { get; }
    private IFileStorageProvider FileStorageProvider { get; }

    public RaythaFunctionApi_V1(ISender mediator, IFileStorageProvider fileStorageProvider)
    {
        Mediator = mediator;
        FileStorageProvider = fileStorageProvider;
    }

    public IQueryResponseDto<ListResultDto<ContentItemDto>> GetContentItems(
        string contentTypeDeveloperName,
        string viewId = "",
        string search = "",
        string filter = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetContentItems.Query
            {
                ContentType = contentTypeDeveloperName,
                ViewId = viewId,
                Search = search,
                Filter = filter,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<ListResultDto<DeletedContentItemDto>> GetDeletedContentItems(
        string contentTypeDeveloperName,
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetDeletedContentItems.Query
            {
                DeveloperName = contentTypeDeveloperName,
                Search = search,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<ContentItemDto> GetContentItemById(string contentItemId)
    {
        return Send(new GetContentItemById.Query { Id = contentItemId });
    }

    public ICommandResponseDto<ShortGuid> CreateContentItem(
        string contentTypeDeveloperName,
        bool saveAsDraft,
        string templateId,
        IDictionary<string, object> content
    )
    {
        return Send(
            new CreateContentItem.Command
            {
                ContentTypeDeveloperName = contentTypeDeveloperName,
                Content = content.ToDictionary(entry => entry.Key, entry => entry.Value),
                SaveAsDraft = saveAsDraft,
                TemplateId = templateId,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> EditContentItem(
        string contentItemId,
        bool saveAsDraft,
        IDictionary<string, object> content
    )
    {
        return Send(
            new EditContentItem.Command
            {
                Id = contentItemId,
                SaveAsDraft = saveAsDraft,
                Content = content.ToDictionary(entry => entry.Key, entry => entry.Value),
            }
        );
    }

    public ICommandResponseDto<ShortGuid> EditContentItemSettings(
        string contentItemId,
        string templateId,
        string routePath
    )
    {
        return Send(
            new EditContentItemSettings.Command
            {
                Id = contentItemId,
                TemplateId = templateId,
                RoutePath = routePath,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> UnpublishContentItem(string contentItemId)
    {
        return Send(new UnpublishContentItem.Command { Id = contentItemId });
    }

    public ICommandResponseDto<ShortGuid> DeleteContentItem(string contentItemId)
    {
        return Send(new DeleteContentItem.Command { Id = contentItemId });
    }

    public IQueryResponseDto<RouteDto> GetRouteByPath(string routePath)
    {
        return Send(new GetRouteByPath.Query { Path = routePath });
    }

    public IQueryResponseDto<ListResultDto<ContentTypeDto>> GetContentTypes(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetContentTypes.Query
            {
                Search = search,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<ContentTypeDto> GetContentTypeByDeveloperName(
        string contentTypeDeveloperName
    )
    {
        return Send(
            new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName }
        );
    }

    public IQueryResponseDto<ListResultDto<MediaItemDto>> GetMediaItems(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetMediaItems.Query
            {
                Search = search,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<string> GetMediaItemUrlByObjectKey(string objectKey)
    {
        return ExecuteSync(async () =>
        {
            var downloadUrl = await FileStorageProvider
                .GetDownloadUrlAsync(objectKey, FileStorageUtility.GetDefaultExpiry())
                .ConfigureAwait(false);
            return new QueryResponseDto<string>(downloadUrl);
        });
    }

    public IQueryResponseDto<ListResultDto<UserGroupDto>> GetUserGroups(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetUserGroups.Query
            {
                Search = search,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<UserGroupDto> GetUserGroupById(string userGroupId)
    {
        return Send(new GetUserGroupById.Query { Id = userGroupId });
    }

    public ICommandResponseDto<ShortGuid> CreateUserGroup(string developerName, string label)
    {
        return Send(new CreateUserGroup.Command { DeveloperName = developerName, Label = label });
    }

    public ICommandResponseDto<ShortGuid> EditUserGroup(string userGroupId, string label)
    {
        return Send(new EditUserGroup.Command { Id = userGroupId, Label = label });
    }

    public ICommandResponseDto<ShortGuid> DeleteUserGroup(string userGroupId)
    {
        return Send(new DeleteUserGroup.Command { Id = userGroupId });
    }

    public IQueryResponseDto<ListResultDto<UserDto>> GetUsers(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetUsers.Query
            {
                Search = search,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<UserDto> GetUserById(string userId)
    {
        return Send(new GetUserById.Query { Id = userId });
    }

    public ICommandResponseDto<ShortGuid> CreateUser(
        string emailAddress,
        string firstName,
        string lastName,
        bool sendEmail,
        dynamic userGroups
    )
    {
        var userGroupsAsEnum = new List<ShortGuid>();
        for (var index = 0; index < (int)userGroups["length"]; index++)
        {
            userGroupsAsEnum.Add(userGroups[index]);
        }
        return Send(
            new CreateUser.Command
            {
                EmailAddress = emailAddress,
                FirstName = firstName,
                LastName = lastName,
                SendEmail = sendEmail,
                UserGroups = userGroupsAsEnum,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> EditUser(
        string userId,
        string emailAddress,
        string firstName,
        string lastName,
        dynamic userGroups
    )
    {
        var userGroupsAsEnum = new List<ShortGuid>();
        for (var index = 0; index < (int)userGroups["length"]; index++)
        {
            userGroupsAsEnum.Add(userGroups[index]);
        }
        return Send(
            new EditUser.Command
            {
                Id = userId,
                EmailAddress = emailAddress,
                FirstName = firstName,
                LastName = lastName,
                UserGroups = userGroupsAsEnum,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> DeleteUser(string userId)
    {
        return Send(new DeleteUser.Command { Id = userId });
    }

    public ICommandResponseDto<ShortGuid> ResetPassword(
        string userId,
        bool sendEmail,
        string newPassword
    )
    {
        return Send(
            new ResetPassword.Command
            {
                Id = userId,
                SendEmail = sendEmail,
                NewPassword = newPassword,
                ConfirmNewPassword = newPassword,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> SetIsActive(string userId, bool isActive)
    {
        return Send(new SetIsActive.Command { Id = userId, IsActive = isActive });
    }

    public IQueryResponseDto<ListResultDto<WebTemplateDto>> GetWebTemplates(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetWebTemplates.Query
            {
                Search = search,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<WebTemplateDto> GetWebTemplateById(string webTemplateId)
    {
        return Send(new GetWebTemplateById.Query { Id = webTemplateId });
    }

    public ICommandResponseDto<object> ExecuteRaythaFunction(
        string developerName,
        string requestMethod,
        string queryJson,
        string payloadJson
    )
    {
        return Send(
            new ExecuteRaythaFunction.Command
            {
                DeveloperName = developerName,
                RequestMethod = requestMethod,
                QueryJson = queryJson,
                PayloadJson = payloadJson,
            }
        );
    }

    // SitePage methods
    public IQueryResponseDto<ListResultDto<SitePageDto>> GetSitePages(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetSitePages.Query
            {
                Search = search,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<SitePageDto> GetSitePageById(string sitePageId)
    {
        return Send(new GetSitePageById.Query { Id = sitePageId });
    }

    public ICommandResponseDto<ShortGuid> CreateSitePage(
        string title,
        bool saveAsDraft,
        string templateId
    )
    {
        return Send(
            new CreateSitePage.Command
            {
                Title = title,
                SaveAsDraft = saveAsDraft,
                TemplateId = templateId,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> EditSitePage(
        string sitePageId,
        string title,
        bool saveAsDraft,
        string templateId
    )
    {
        return Send(
            new EditSitePage.Command
            {
                Id = sitePageId,
                Title = title,
                SaveAsDraft = saveAsDraft,
                TemplateId = templateId,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> EditSitePageSettings(string sitePageId, string routePath)
    {
        return Send(new EditSitePageSettings.Command { Id = sitePageId, RoutePath = routePath });
    }

    public ICommandResponseDto<ShortGuid> PublishSitePage(string sitePageId)
    {
        return Send(new PublishSitePage.Command { Id = sitePageId });
    }

    public ICommandResponseDto<ShortGuid> UnpublishSitePage(string sitePageId)
    {
        return Send(new UnpublishSitePage.Command { Id = sitePageId });
    }

    public ICommandResponseDto<ShortGuid> DeleteSitePage(string sitePageId)
    {
        return Send(new DeleteSitePage.Command { Id = sitePageId });
    }

    // NavigationMenu methods
    public IQueryResponseDto<ListResultDto<NavigationMenuDto>> GetNavigationMenus(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        return Send(
            new GetNavigationMenus.Query
            {
                Search = search,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            }
        );
    }

    public IQueryResponseDto<NavigationMenuDto> GetNavigationMenuById(string navigationMenuId)
    {
        return Send(new GetNavigationMenuById.Query { Id = navigationMenuId });
    }

    public IQueryResponseDto<NavigationMenuDto> GetNavigationMenuByDeveloperName(
        string developerName
    )
    {
        return Send(new GetNavigationMenuByDeveloperName.Query { DeveloperName = developerName });
    }

    public ICommandResponseDto<ShortGuid> CreateNavigationMenu(string label, string developerName)
    {
        return Send(
            new CreateNavigationMenu.Command { Label = label, DeveloperName = developerName }
        );
    }

    public ICommandResponseDto<ShortGuid> EditNavigationMenu(string navigationMenuId, string label)
    {
        return Send(new EditNavigationMenu.Command { Id = navigationMenuId, Label = label });
    }

    public ICommandResponseDto<ShortGuid> DeleteNavigationMenu(string navigationMenuId)
    {
        return Send(new DeleteNavigationMenu.Command { Id = navigationMenuId });
    }

    // NavigationMenuItem methods
    public IQueryResponseDto<
        IReadOnlyCollection<NavigationMenuItemDto>
    > GetNavigationMenuItemsByNavigationMenuId(string navigationMenuId)
    {
        return Send(
            new GetNavigationMenuItemsByNavigationMenuId.Query
            {
                NavigationMenuId = navigationMenuId,
            }
        );
    }

    public IQueryResponseDto<NavigationMenuItemDto> GetNavigationMenuItemById(
        string navigationMenuItemId
    )
    {
        return Send(new GetNavigationMenuItemById.Query { Id = navigationMenuItemId });
    }

    public ICommandResponseDto<ShortGuid> CreateNavigationMenuItem(
        string navigationMenuId,
        string label,
        string url,
        bool isDisabled,
        bool openInNewTab,
        string cssClassName,
        string parentNavigationMenuItemId
    )
    {
        return Send(
            new CreateNavigationMenuItem.Command
            {
                NavigationMenuId = navigationMenuId,
                Label = label,
                Url = url,
                IsDisabled = isDisabled,
                OpenInNewTab = openInNewTab,
                CssClassName = cssClassName,
                ParentNavigationMenuItemId = string.IsNullOrEmpty(parentNavigationMenuItemId)
                    ? null
                    : (ShortGuid)parentNavigationMenuItemId,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> EditNavigationMenuItem(
        string navigationMenuItemId,
        string navigationMenuId,
        string label,
        string url,
        bool isDisabled,
        bool openInNewTab,
        string cssClassName,
        string parentNavigationMenuItemId
    )
    {
        return Send(
            new EditNavigationMenuItem.Command
            {
                Id = navigationMenuItemId,
                NavigationMenuId = navigationMenuId,
                Label = label,
                Url = url,
                IsDisabled = isDisabled,
                OpenInNewTab = openInNewTab,
                CssClassName = cssClassName,
                ParentNavigationMenuItemId = string.IsNullOrEmpty(parentNavigationMenuItemId)
                    ? null
                    : (ShortGuid)parentNavigationMenuItemId,
            }
        );
    }

    public ICommandResponseDto<ShortGuid> DeleteNavigationMenuItem(
        string navigationMenuItemId,
        string navigationMenuId
    )
    {
        return Send(
            new DeleteNavigationMenuItem.Command
            {
                Id = navigationMenuItemId,
                NavigationMenuId = navigationMenuId,
            }
        );
    }

    private TResponse Send<TResponse>(IRequest<TResponse> request)
    {
        return ExecuteSync(async () => await Mediator.Send(request).ConfigureAwait(false));
    }

    private static T ExecuteSync<T>(Func<Task<T>> operation)
    {
        return Task.Run(operation).GetAwaiter().GetResult();
    }
}
