using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.MediaItems;
using Raytha.Application.MediaItems.Queries;
using Raytha.Application.Routes;
using Raytha.Application.Routes.Queries;
using Raytha.Application.Templates.Web;
using Raytha.Application.Templates.Web.Queries;
using Raytha.Application.UserGroups;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;
using Raytha.Application.Users;
using Raytha.Application.Common.Utils;

namespace Raytha.Infrastructure.RaythaFunctions;

public class RaythaFunctionApi : IRaythaFunctionApi
{
    private ISender Mediator { get; }
    private IFileStorageProvider FileStorageProvider { get; }

    public RaythaFunctionApi(ISender mediator, IFileStorageProvider fileStorageProvider)
    {
        Mediator = mediator;
        FileStorageProvider = fileStorageProvider;
    }

    public async Task<IQueryResponseDto<ListResultDto<ContentItemDto>>> GetContentItemsAsync(string contentTypeDeveloperName, string viewId = "", string search = "", string filter = "",
        string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return await Mediator.Send(new GetContentItems.Query
        {
            ContentType = contentTypeDeveloperName,
            ViewId = viewId,
            Search = search,
            Filter = filter,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<IQueryResponseDto<ListResultDto<DeletedContentItemDto>>> GetDeletedContentItemsAsync(string contentTypeDeveloperName)
    {
        return await Mediator.Send(new GetDeletedContentItems.Query
        {
            DeveloperName = contentTypeDeveloperName,
        });
    }

    public async Task<IQueryResponseDto<ContentItemDto>> GetContentItemByIdAsync(ShortGuid contentItemId)
    {
        return await Mediator.Send(new GetContentItemById.Query
        {
            Id = contentItemId,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> CreateContentItemAsync(string contentTypeDeveloperName, bool saveAsDraft, ShortGuid templateId, IDictionary<string, object> content)
    {
        return await Mediator.Send(new CreateContentItem.Command
        {
            ContentTypeDeveloperName = contentTypeDeveloperName,
            Content = content,
            SaveAsDraft = saveAsDraft,
            TemplateId = templateId,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> EditContentItemAsync(ShortGuid contentItemId, bool saveAsDraft, IDictionary<string, object> content)
    {
        return await Mediator.Send(new EditContentItem.Command
        {
            Id = contentItemId,
            SaveAsDraft = saveAsDraft,
            Content = content,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> EditContentItemSettingsAsync(ShortGuid contentItemId, ShortGuid templateId, string routePath)
    {
        return await Mediator.Send(new EditContentItemSettings.Command
        {
            Id = contentItemId,
            TemplateId = templateId,
            RoutePath = routePath,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> UnpublishContentItemAsync(ShortGuid contentItemId)
    {
        return await Mediator.Send(new UnpublishContentItem.Command
        {
            Id = contentItemId,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> DeleteContentItemAsync(ShortGuid contentItemId)
    {
        return await Mediator.Send(new DeleteContentItem.Command
        {
            Id = contentItemId,
        });
    }

    public async Task<IQueryResponseDto<RouteDto>> GetRouteByPathAsync(string routePath)
    {
        return await Mediator.Send(new GetRouteByPath.Query
        {
            Path = routePath,
        });
    }

    public async Task<IQueryResponseDto<ListResultDto<ContentTypeDto>>> GetContentTypesAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return await Mediator.Send(new GetContentTypes.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });
    }

    public async Task<IQueryResponseDto<ContentTypeDto>> GetContentTypeByDeveloperNameAsync(string contentTypeDeveloperName)
    {
        return await Mediator.Send(new GetContentTypeByDeveloperName.Query
        {
            DeveloperName = contentTypeDeveloperName,
        });
    }

    public async Task<IQueryResponseDto<ListResultDto<MediaItemDto>>> GetMediaItemsAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return await Mediator.Send(new GetMediaItems.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });
    }

    public async Task<IQueryResponseDto<string>> GetMediaItemUrlByObjectKeyAsync(string objectKey)
    {
        return new QueryResponseDto<string>(await FileStorageProvider.GetDownloadUrlAsync(objectKey, FileStorageUtility.GetDefaultExpiry()));
    }

    public async Task<IQueryResponseDto<ListResultDto<UserGroupDto>>> GetUserGroupsAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return await Mediator.Send(new GetUserGroups.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });
    }

    public async Task<IQueryResponseDto<UserGroupDto>> GetUserGroupByIdAsync(ShortGuid userGroupId)
    {
        return await Mediator.Send(new GetUserGroupById.Query
        {
            Id = userGroupId,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> CreateUserGroupAsync(string developerName, string label)
    {
        return await Mediator.Send(new CreateUserGroup.Command
        {
            DeveloperName = developerName,
            Label = label,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> EditUserGroupAsync(ShortGuid userGroupId, string label)
    {
        return await Mediator.Send(new EditUserGroup.Command
        {
            Id = userGroupId,
            Label = label,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> DeleteUserGroupAsync(ShortGuid userGroupId)
    {
        return await Mediator.Send(new DeleteUserGroup.Command
        {
            Id = userGroupId,
        });
    }

    public async Task<IQueryResponseDto<ListResultDto<UserDto>>> GetUsersAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return await Mediator.Send(new GetUsers.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });

    }

    public async Task<IQueryResponseDto<UserDto>> GetUserByIdAsync(ShortGuid userId)
    {
        return await Mediator.Send(new GetUserById.Query
        {
            Id = userId
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> CreateUserAsync(string emailAddress, string firstName, string lastName, bool sendEmail, IEnumerable<ShortGuid> userGroups)
    {
        return await Mediator.Send(new CreateUser.Command
        {
            EmailAddress = emailAddress,
            FirstName = firstName,
            LastName = lastName,
            SendEmail = sendEmail,
            UserGroups = userGroups,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> EditUserAsync(ShortGuid userId, string emailAddress, string firstName, string lastName, IEnumerable<ShortGuid> userGroups)
    {
        return await Mediator.Send(new EditUser.Command
        {
            Id = userId,
            EmailAddress = emailAddress,
            FirstName = firstName,
            LastName = lastName,
            UserGroups = userGroups,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> DeleteUserAsync(ShortGuid userId)
    {
        return await Mediator.Send(new DeleteUser.Command
        {
            Id = userId
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> ResetPasswordAsync(ShortGuid userId, bool sendEmail, string newPassword)
    {
        return await Mediator.Send(new ResetPassword.Command
        {
            Id = userId,
            SendEmail = sendEmail,
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword,
        });
    }

    public async Task<ICommandResponseDto<ShortGuid>> SetIsActiveAsync(ShortGuid userId, bool isActive)
    {
        return await Mediator.Send(new SetIsActive.Command
        {
            Id = userId,
            IsActive = isActive,
        });
    }

    public async Task<IQueryResponseDto<ListResultDto<WebTemplateDto>>> GetWebTemplatesAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return await Mediator.Send(new GetWebTemplates.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        });
    }

    public async Task<IQueryResponseDto<WebTemplateDto>> GetWebTemplateByIdAsync(ShortGuid webTemplateId)
    {
        return await Mediator.Send(new GetWebTemplateById.Query
        {
            Id = webTemplateId
        });
    }
}