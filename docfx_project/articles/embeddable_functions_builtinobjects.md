# Built-in Objects for Raytha Functions

Raytha Functions provides you with access to the service layer and other functionality so that you can build more powerful applications.

| Object                             | Notes              |
| ------------------------------     | ------------------|
| API_V1                             | API to Raytha's Service Layer |
| CurrentOrganization                | Object with access to the current org settings like name, timezone, etc. |
| CurrentUser                        | Information about the current logged in user |
| Emailer                            | Send an email from the platform |
| HttpClient                         | Make an external API call |

## API_V1

API_V1 is set to mirror the functionality offered by the out of the box Headless REST API that you can see at your website's /raytha/api route.

A very basic example usage is shown below.

```
function get(query) {
    var response = API_V1.GetContentItems('posts');
    return new JsonResult(response);
}

function post(payload, query) {
    let bobsValue = payload.find(item => item.Key === "bob").Value[0];
    let janesValue = payload.find(item => item.Key === "jane").Value[0];
    let content = {
        'title': bobsValue,
        'tinymce': janesValue
    };
    let detail_view_template_id = '1vVCcIYeeE-dMQjbYGq_ng';
    let result = API_V1.CreateContentItem('posts', false, detail_view_template_id, content);
    return new JsonResult(result);
}
```

In the code base, you can find the API in Raytha.Infrastructure > RaythaFunctions > RaythaFunctionApi_V1.cs if you plan to modify the code base and add functions here.

The out of the box function definitions are:

```
//Content items
IQueryResponseDto<ListResultDto<ContentItemDto>> GetContentItems(string contentTypeDeveloperName, string viewId = "", string search = "", string filter = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
IQueryResponseDto<ListResultDto<DeletedContentItemDto>> GetDeletedContentItems(string contentTypeDeveloperName, string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
IQueryResponseDto<ContentItemDto> GetContentItemById(string contentItemId)
ICommandResponseDto<ShortGuid> CreateContentItem(string contentTypeDeveloperName, bool saveAsDraft, string templateId, IDictionary<string, object> content)
ICommandResponseDto<ShortGuid> EditContentItem(string contentItemId, bool saveAsDraft, IDictionary<string, object> content)
ICommandResponseDto<ShortGuid> EditContentItemSettings(string contentItemId, string templateId, string routePath)
ICommandResponseDto<ShortGuid> UnpublishContentItem(string contentItemId)
ICommandResponseDto<ShortGuid> DeleteContentItem(string contentItemId)
IQueryResponseDto<RouteDto> GetRouteByPath(string routePath)

//Content types
IQueryResponseDto<ListResultDto<ContentTypeDto>> GetContentTypes(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
IQueryResponseDto<ContentTypeDto> GetContentTypeByDeveloperName(string contentTypeDeveloperName)

//Media items
IQueryResponseDto<ListResultDto<MediaItemDto>> GetMediaItems(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
IQueryResponseDto<string> GetMediaItemUrlByObjectKey(string objectKey)

//User groups
IQueryResponseDto<ListResultDto<UserGroupDto>> GetUserGroups(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
IQueryResponseDto<UserGroupDto> GetUserGroupById(string userGroupId)
ICommandResponseDto<ShortGuid> CreateUserGroup(string developerName, string label)
ICommandResponseDto<ShortGuid> EditUserGroup(string userGroupId, string label)
ICommandResponseDto<ShortGuid> DeleteUserGroup(string userGroupId)

//Users
IQueryResponseDto<ListResultDto<UserDto>> GetUsers(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
IQueryResponseDto<UserDto> GetUserById(string userId)
ICommandResponseDto<ShortGuid> CreateUser(string emailAddress, string firstName, string lastName, bool sendEmail, dynamic userGroups)
ICommandResponseDto<ShortGuid> EditUser(string userId, string emailAddress, string firstName, string lastName, dynamic userGroups)
ICommandResponseDto<ShortGuid> DeleteUser(string userId)
ICommandResponseDto<ShortGuid> ResetPassword(string userId, bool sendEmail, string newPassword)
ICommandResponseDto<ShortGuid> SetIsActive(string userId, bool isActive)

//Templates
IQueryResponseDto<ListResultDto<WebTemplateDto>> GetWebTemplates(string search = "", string orderBy = "", int pageNumber = 1, int pageSize = 50)
IQueryResponseDto<WebTemplateDto> GetWebTemplateById(string webTemplateId)

//Functions
ICommandResponseDto<object> ExecuteRaythaFunction(string developerName, string requestMethod, string queryJson, string payloadJson)
```

## CurrentUser

`CurrentUser` offers the same information that you might find if you are creating a template and are inserting a variable for the CurrentUser object. A good way to see all of the output available for CurrentUser is to run this GET request.

```
function get(query) {
    return new JsonResult(CurrentUser);
}
```

Information includes name, email, whether they are authenticated or not, permissions & roles, user groups, if they are an admin, and their authentication method.

## CurrentOrganization

`CurrentOrganization` offers the same information that you might find if you are creating a template and are inserting a variable for the CurrentOrganization object. A good way to see all of the output available for CurrentUser is to run this GET request.

```
function get(query) {
    return new JsonResult(CurrentOrganization);
}
```

Information includes available authentication methods, available content types and their field configs, organization name, default SMTP from address, system timezone, and other info.

## Emailer

Raytha gives access to the Emailer, which is the same Emailer used for other email functionality such as resetting password and registration emails. You might find it useful to have access to this emailer for scenarios such as sending an email when you receive a form submission into a `post()`.

You need to provide the subject line, content, email of recipient, and reply-to information, which you could also grab from `CurrentOrganization.SmtpDefaultFromName` and `CurrentOrganization.SmtpDefaultFromAddress` if you prefer.

You provide this information to `EmailMessage.From(subject, content, recipientEmail, fromEmail, fromName)`;

You then use `Emailer.SendEmail(emailMsg)` to send the email out. See the example below.

```
function post(payload, query) {
    var emailMsg = EmailMessage.From("Test subject", "Test content", "test@test.com", "me@test.com", "me");
    Emailer.SendEmail(emailMsg);
    return new JsonResult({ success: true });
}
```

## HttpClient

Making api calls to external services is an essential part of any application. Raytha provides a wrapper over .NET's HttpClient. The wrapper includes a method for each of the main HTTP methods.

```
Get(string url, IDictionary<string, object> headers = null)
Post(string url, IDictionary<string, object> headers = null, IDictionary<string, object> body = null, bool json = true)
Put(string url, IDictionary<string, object> headers = null, IDictionary<string, object> body = null, bool json = true)
Delete(string url, IDictionary<string, object> headers = null)
```

The example below is a sample for making an API call to api2pdf.com's API for generating a PDF file.

```
function get(query) {
    var payload = {
        "html": "<p>Hello World</p>"
    };
    var headers = {
        "Authorization": "YOUR-API-KEY-HERE"
    };
    var response = HttpClient.Post("https://v2.api2pdf.com/chrome/pdf/html", headers=headers, body=payload);
    result = JSON.parse(response);
    return new RedirectResult(result.FileUrl);
}
```

