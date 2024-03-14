using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentTypes;
using Raytha.Application.Routes;
using Raytha.Application.MediaItems;
using Raytha.Application.Templates.Web;
using Raytha.Application.UserGroups;
using Raytha.Application.Users;

namespace Raytha.Application.Common.Interfaces;

public interface IRaythaFunctionApi
{
    public Task<IQueryResponseDto<ListResultDto<ContentItemDto>>> GetContentItemsAsync(string contentTypeDeveloperName, string viewId = "", string search = "", string filter = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public Task<IQueryResponseDto<ListResultDto<DeletedContentItemDto>>> GetDeletedContentItemsAsync(string contentTypeDeveloperName);
    public Task<IQueryResponseDto<ContentItemDto>> GetContentItemByIdAsync(ShortGuid contentItemId);
    public Task<ICommandResponseDto<ShortGuid>> CreateContentItemAsync(string contentTypeDeveloperName, bool saveAsDraft, ShortGuid templateId, IDictionary<string, object> content);
    public Task<ICommandResponseDto<ShortGuid>> EditContentItemAsync(ShortGuid contentItemId, bool saveAsDraft, IDictionary<string, object> content);
    public Task<ICommandResponseDto<ShortGuid>> EditContentItemSettingsAsync(ShortGuid contentItemId, ShortGuid templateId, string routePath);
    public Task<ICommandResponseDto<ShortGuid>> UnpublishContentItemAsync(ShortGuid contentItemId);
    public Task<ICommandResponseDto<ShortGuid>> DeleteContentItemAsync(ShortGuid contentItemId);
    public Task<IQueryResponseDto<RouteDto>> GetRouteByPathAsync(string routePath);

    public Task<IQueryResponseDto<ListResultDto<ContentTypeDto>>> GetContentTypesAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public Task<IQueryResponseDto<ContentTypeDto>> GetContentTypeByDeveloperNameAsync(string contentTypeDeveloperName);

    public Task<IQueryResponseDto<ListResultDto<MediaItemDto>>> GetMediaItemsAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public Task<IQueryResponseDto<string>> GetMediaItemUrlByObjectKeyAsync(string objectKey);

    public Task<IQueryResponseDto<ListResultDto<UserGroupDto>>> GetUserGroupsAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public Task<IQueryResponseDto<UserGroupDto>> GetUserGroupByIdAsync(ShortGuid userGroupId);
    public Task<ICommandResponseDto<ShortGuid>> CreateUserGroupAsync(string developerName, string label);
    public Task<ICommandResponseDto<ShortGuid>> EditUserGroupAsync(ShortGuid userGroupId, string label);
    public Task<ICommandResponseDto<ShortGuid>> DeleteUserGroupAsync(ShortGuid userGroupId);

    public Task<IQueryResponseDto<ListResultDto<UserDto>>> GetUsersAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public Task<IQueryResponseDto<UserDto>> GetUserByIdAsync(ShortGuid userId);
    public Task<ICommandResponseDto<ShortGuid>> CreateUserAsync(string emailAddress, string firstName, string lastName, bool sendEmail, IEnumerable<ShortGuid> userGroups);
    public Task<ICommandResponseDto<ShortGuid>> EditUserAsync(ShortGuid userId, string emailAddress, string firstName, string lastName, IEnumerable<ShortGuid> userGroups);
    public Task<ICommandResponseDto<ShortGuid>> DeleteUserAsync(ShortGuid userId);
    public Task<ICommandResponseDto<ShortGuid>> ResetPasswordAsync(ShortGuid userId, bool sendEmail, string newPassword);
    public Task<ICommandResponseDto<ShortGuid>> SetIsActiveAsync(ShortGuid userId, bool isActive);

    public Task<IQueryResponseDto<ListResultDto<WebTemplateDto>>> GetWebTemplatesAsync(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50);
    public Task<IQueryResponseDto<WebTemplateDto>> GetWebTemplateByIdAsync(ShortGuid webTemplateId);
}