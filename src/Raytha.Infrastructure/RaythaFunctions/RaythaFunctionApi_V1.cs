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
using Raytha.Application.UserGroups;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;
using Raytha.Application.Users;
using Raytha.Application.Common.Utils;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WebTemplates.Queries;

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

    public IQueryResponseDto<ListResultDto<ContentItemDto>> GetContentItems(string contentTypeDeveloperName, string viewId = "", string search = "", string filter = "",
        string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return Mediator.Send(new GetContentItems.Query
        {
            ContentType = contentTypeDeveloperName,
            ViewId = viewId,
            Search = search,
            Filter = filter,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize
        }).Result;
    }

    public IQueryResponseDto<ListResultDto<DeletedContentItemDto>> GetDeletedContentItems(string contentTypeDeveloperName, string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return Mediator.Send(new GetDeletedContentItems.Query
        {
            DeveloperName = contentTypeDeveloperName,
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize
        }).Result;
    }

    public IQueryResponseDto<ContentItemDto> GetContentItemById(string contentItemId)
    {
        return  Mediator.Send(new GetContentItemById.Query
        {
            Id = contentItemId,
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> CreateContentItem(string contentTypeDeveloperName, bool saveAsDraft, string templateId, IDictionary<string, object> content)
    {
        
        return Mediator.Send(new CreateContentItem.Command
        {
            ContentTypeDeveloperName = contentTypeDeveloperName,
            Content = content.ToDictionary(entry => entry.Key, entry => entry.Value),
            SaveAsDraft = saveAsDraft,
            TemplateId = templateId,
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> EditContentItem(string contentItemId, bool saveAsDraft, IDictionary<string, object> content)
    {
        return Mediator.Send(new EditContentItem.Command
        {
            Id = contentItemId,
            SaveAsDraft = saveAsDraft,
            Content = content.ToDictionary(entry => entry.Key, entry => entry.Value),
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> EditContentItemSettings(string contentItemId, string templateId, string routePath)
    {
        return Mediator.Send(new EditContentItemSettings.Command
        {
            Id = contentItemId,
            TemplateId = templateId,
            RoutePath = routePath,
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> UnpublishContentItem(string contentItemId)
    {
        return Mediator.Send(new UnpublishContentItem.Command
        {
            Id = contentItemId,
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> DeleteContentItem(string contentItemId)
    {
        return Mediator.Send(new DeleteContentItem.Command
        {
            Id = contentItemId,
        }).Result;
    }

    public IQueryResponseDto<RouteDto> GetRouteByPath(string routePath)
    {
        return Mediator.Send(new GetRouteByPath.Query
        {
            Path = routePath,
        }).Result;
    }

    public IQueryResponseDto<ListResultDto<ContentTypeDto>> GetContentTypes(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return Mediator.Send(new GetContentTypes.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }).Result;
    }

    public IQueryResponseDto<ContentTypeDto> GetContentTypeByDeveloperName(string contentTypeDeveloperName)
    {
        return Mediator.Send(new GetContentTypeByDeveloperName.Query
        {
            DeveloperName = contentTypeDeveloperName,
        }).Result;
    }

    public IQueryResponseDto<ListResultDto<MediaItemDto>> GetMediaItems(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return Mediator.Send(new GetMediaItems.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }).Result;
    }

    public IQueryResponseDto<string> GetMediaItemUrlByObjectKey(string objectKey)
    {
        return new QueryResponseDto<string>(FileStorageProvider.GetDownloadUrlAsync(objectKey, FileStorageUtility.GetDefaultExpiry()).Result);
    }

    public IQueryResponseDto<ListResultDto<UserGroupDto>> GetUserGroups(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return Mediator.Send(new GetUserGroups.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }).Result;
    }

    public IQueryResponseDto<UserGroupDto> GetUserGroupById(string userGroupId)
    {
        return Mediator.Send(new GetUserGroupById.Query
        {
            Id = userGroupId,
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> CreateUserGroup(string developerName, string label)
    {
        return Mediator.Send(new CreateUserGroup.Command
        {
            DeveloperName = developerName,
            Label = label,
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> EditUserGroup(string userGroupId, string label)
    {
        return Mediator.Send(new EditUserGroup.Command
        {
            Id = userGroupId,
            Label = label,
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> DeleteUserGroup(string userGroupId)
    {
        return Mediator.Send(new DeleteUserGroup.Command
        {
            Id = userGroupId,
        }).Result;
    }

    public IQueryResponseDto<ListResultDto<UserDto>> GetUsers(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return Mediator.Send(new GetUsers.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }).Result;

    }

    public IQueryResponseDto<UserDto> GetUserById(string userId)
    {
        return Mediator.Send(new GetUserById.Query
        {
            Id = userId
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> CreateUser(string emailAddress, string firstName, string lastName, bool sendEmail, dynamic userGroups)
    {
        var userGroupsAsEnum = new List<ShortGuid>();
        for (var index = 0; index < (int)userGroups["length"]; index++)
        {
            userGroupsAsEnum.Add(userGroups[index]);
        }
        return Mediator.Send(new CreateUser.Command
        {
            EmailAddress = emailAddress,
            FirstName = firstName,
            LastName = lastName,
            SendEmail = sendEmail,
            UserGroups = userGroupsAsEnum
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> EditUser(string userId, string emailAddress, string firstName, string lastName, dynamic userGroups)
    {
        var userGroupsAsEnum = new List<ShortGuid>();
        for (var index = 0; index < (int)userGroups["length"]; index++)
        {
            userGroupsAsEnum.Add(userGroups[index]);
        }
        return Mediator.Send(new EditUser.Command
        {
            Id = userId,
            EmailAddress = emailAddress,
            FirstName = firstName,
            LastName = lastName,
            UserGroups = userGroupsAsEnum
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> DeleteUser(string userId)
    {
        return Mediator.Send(new DeleteUser.Command
        {
            Id = userId
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> ResetPassword(string userId, bool sendEmail, string newPassword)
    {
        return Mediator.Send(new ResetPassword.Command
        {
            Id = userId,
            SendEmail = sendEmail,
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword,
        }).Result;
    }

    public ICommandResponseDto<ShortGuid> SetIsActive(string userId, bool isActive)
    {
        return Mediator.Send(new SetIsActive.Command
        {
            Id = userId,
            IsActive = isActive,
        }).Result;
    }

    public IQueryResponseDto<ListResultDto<WebTemplateDto>> GetWebTemplates(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
    {
        return Mediator.Send(new GetWebTemplates.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }).Result;
    }

    public IQueryResponseDto<WebTemplateDto> GetWebTemplateById(string webTemplateId)
    {
        return Mediator.Send(new GetWebTemplateById.Query
        {
            Id = webTemplateId
        }).Result;
    }

    public ICommandResponseDto<object> ExecuteRaythaFunction(string developerName, string requestMethod, string queryJson, string payloadJson)
    {
        return Mediator.Send(new ExecuteRaythaFunction.Command
        {
            DeveloperName = developerName,
            RequestMethod = requestMethod,
            QueryJson = queryJson,
            PayloadJson = payloadJson
        }).Result;
    }
}