using Raytha.Application.Common.Models;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentTypes;
using Raytha.Application.Routes;
using Raytha.Application.MediaItems;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.UserGroups;
using Raytha.Application.Users;
using CSharpVitamins;

namespace Raytha.Application.Common.Interfaces;

public interface IRaythaFunctionApi_V1
{
    public IQueryResponseDto<ListResultDto<ContentItemDto>> GetContentItems(string contentTypeDeveloperName, string viewId = "", string search = "", string filter = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public IQueryResponseDto<ListResultDto<DeletedContentItemDto>> GetDeletedContentItems(string contentTypeDeveloperName, string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public IQueryResponseDto<ContentItemDto> GetContentItemById(string contentItemId);
    public ICommandResponseDto<ShortGuid> CreateContentItem(string contentTypeDeveloperName, bool saveAsDraft, string templateId, IDictionary<string, object> content);
    public ICommandResponseDto<ShortGuid> EditContentItem(string contentItemId, bool saveAsDraft, IDictionary<string, object> content);
    public ICommandResponseDto<ShortGuid> EditContentItemSettings(string contentItemId, string templateId, string routePath);
    public ICommandResponseDto<ShortGuid> UnpublishContentItem(string contentItemId);
    public ICommandResponseDto<ShortGuid> DeleteContentItem(string contentItemId);
    public IQueryResponseDto<RouteDto> GetRouteByPath(string routePath);

    public IQueryResponseDto<ListResultDto<ContentTypeDto>> GetContentTypes(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public IQueryResponseDto<ContentTypeDto> GetContentTypeByDeveloperName(string contentTypeDeveloperName);

    public IQueryResponseDto<ListResultDto<MediaItemDto>> GetMediaItems(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public IQueryResponseDto<string> GetMediaItemUrlByObjectKey(string objectKey);

    public IQueryResponseDto<ListResultDto<UserGroupDto>> GetUserGroups(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public IQueryResponseDto<UserGroupDto> GetUserGroupById(string userGroupId);
    public ICommandResponseDto<ShortGuid> CreateUserGroup(string developerName, string label);
    public ICommandResponseDto<ShortGuid> EditUserGroup(string userGroupId, string label);
    public ICommandResponseDto<ShortGuid> DeleteUserGroup(string userGroupId);

    public IQueryResponseDto<ListResultDto<UserDto>> GetUsers(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public IQueryResponseDto<UserDto> GetUserById(string userId);
    public ICommandResponseDto<ShortGuid> CreateUser(string emailAddress, string firstName, string lastName, bool sendEmail, dynamic userGroups);
    public ICommandResponseDto<ShortGuid> EditUser(string userId, string emailAddress, string firstName, string lastName, dynamic userGroups);
    public ICommandResponseDto<ShortGuid> DeleteUser(string userId);
    public ICommandResponseDto<ShortGuid> ResetPassword(string userId, bool sendEmail, string newPassword);
    public ICommandResponseDto<ShortGuid> SetIsActive(string userId, bool isActive);

    public IQueryResponseDto<ListResultDto<WebTemplateDto>> GetWebTemplates(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public IQueryResponseDto<WebTemplateDto> GetWebTemplateById(string webTemplateId);

    public ICommandResponseDto<object> ExecuteRaythaFunction(string developerName, string requestMethod, string queryJson, string payloadJson);
}