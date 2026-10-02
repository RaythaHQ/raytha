using Raytha.Application.ContentTypes;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.Common.Utils;

public record TemplateVariableDto
{
    public required string Path { get; init; }
    public string? Description { get; init; }
    public string? Example { get; init; }
}

public record TemplateVariableGroupDto
{
    public required string Category { get; init; }
    public required IReadOnlyList<TemplateVariableDto> Variables { get; init; }
}

public static class TemplateInsertVariables
{
    public static IReadOnlyList<TemplateVariableGroupDto> ForEmail(string? developerName)
    {
        var groups = new List<TemplateVariableGroupDto>();
        Add(groups, "Organization", InsertVariableTemplateFactory.CurrentOrganization);
        if (
            !string.IsNullOrEmpty(developerName)
            && InsertVariableTemplateFactory.TryFrom(developerName, out var target)
            && target != null
        )
        {
            Add(groups, "Email data", target);
        }

        return groups;
    }

    public static IReadOnlyList<TemplateVariableGroupDto> ForWeb(
        string? developerName,
        bool isBuiltInTemplate,
        IEnumerable<ContentTypeDto> contentTypes
    )
    {
        var groups = new List<TemplateVariableGroupDto>();
        Add(groups, "Request", InsertVariableTemplateFactory.Request);
        Add(groups, "Organization", InsertVariableTemplateFactory.CurrentOrganization);
        Add(groups, "Current user", InsertVariableTemplateFactory.CurrentUser);
        Add(groups, "Menu", InsertVariableTemplateFactory.NavigationMenu);
        Add(groups, "Menu item", InsertVariableTemplateFactory.NavigationMenuItem);

        if (ShowsContentVariables(developerName, isBuiltInTemplate))
        {
            Add(groups, "Content type", InsertVariableTemplateFactory.ContentType);
            Add(groups, "Content list", InsertVariableTemplateFactory.ContentItemListResult);
            Add(groups, "Content item", InsertVariableTemplateFactory.ContentItem);
            foreach (var contentType in contentTypes)
            {
                AddContentTypeFields(groups, contentType);
            }
        }
        else if (
            !string.IsNullOrEmpty(developerName)
            && InsertVariableTemplateFactory.TryFrom(developerName, out var target)
            && target != null
        )
        {
            Add(groups, "Page data", target);
        }

        return groups;
    }

    public static bool ShowsContentVariables(string? developerName, bool isBuiltInTemplate) =>
        !isBuiltInTemplate || developerName == BuiltInWebTemplate._Layout.DeveloperName;

    private static void Add(
        List<TemplateVariableGroupDto> groups,
        string category,
        InsertVariableTemplateFactory entry
    )
    {
        var variables = entry
            .TemplateInfo.GetTemplateVariables()
            .Select(pair => pair.Value)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct()
            .Select(path => new TemplateVariableDto
            {
                Path = path,
                Description = Descriptions.GetValueOrDefault(path),
                Example = Examples.GetValueOrDefault(path),
            })
            .ToList();

        if (variables.Count > 0)
        {
            groups.Add(new TemplateVariableGroupDto { Category = category, Variables = variables });
        }
    }

    private static void AddContentTypeFields(
        List<TemplateVariableGroupDto> groups,
        ContentTypeDto contentType
    )
    {
        var variables = new List<TemplateVariableDto>();
        foreach (var field in contentType.ContentTypeFields)
        {
            var path = $"Target.PublishedContent.{field.DeveloperName}";
            var label = string.IsNullOrEmpty(field.Label) ? field.DeveloperName : field.Label;
            if (field.FieldType?.DeveloperName == BaseFieldType.OneToOneRelationship.DeveloperName)
            {
                variables.Add(
                    new TemplateVariableDto
                    {
                        Path = path,
                        Description = $"{label}. The related item; read its PrimaryField or PublishedContent.",
                    }
                );
                continue;
            }

            if (field.FieldType?.DeveloperName == BaseFieldType.Repeater.DeveloperName)
            {
                variables.Add(RepeaterLoop(path, label, field.SubFields));
                continue;
            }

            variables.Add(
                new TemplateVariableDto { Path = $"{path}.Text", Description = $"{label}, as display text." }
            );
            variables.Add(
                new TemplateVariableDto { Path = $"{path}.Value", Description = $"{label}, as the raw value." }
            );
        }

        if (variables.Count == 0)
        {
            return;
        }

        var name = string.IsNullOrEmpty(contentType.LabelSingular)
            ? contentType.DeveloperName
            : contentType.LabelSingular;
        var category = $"{name} fields";
        if (groups.Any(g => g.Category == category))
        {
            category = $"{name} ({contentType.DeveloperName}) fields";
        }
        groups.Add(new TemplateVariableGroupDto { Category = category, Variables = variables });
    }

    private static TemplateVariableDto RepeaterLoop(
        string path,
        string label,
        IReadOnlyList<FieldDefinition> subFields
    )
    {
        var cells = subFields.Count > 0 ? subFields.Select(s => s.DeveloperName) : ["sub_field"];
        var body = string.Join("\n", cells.Select(name => $"  {{{{ row.{name} }}}}"));
        return new TemplateVariableDto
        {
            Path = $"{path}.Value",
            Description = $"{label}. A list of rows; loop it and read each sub-field by its developer name.",
            Example = $"{{% for row in {path}.Value %}}\n{body}\n{{% endfor %}}",
        };
    }

    private static readonly Dictionary<string, string> Descriptions = new()
    {
        ["PathBase"] = "Base path the site is served under. Prefix site links with it.",
        ["QueryParams"] = "Query string values, e.g. QueryParams.page.",
        ["RequestVerificationToken"] = "Antiforgery token. Forms that post back to Raytha need it.",
        ["ViewData"] = "Extra data the rendering page passes to the template.",
        ["CurrentOrganization.OrganizationName"] = "Site name from Settings.",
        ["CurrentOrganization.WebsiteUrl"] = "Public URL of the site.",
        ["CurrentOrganization.TimeZone"] = "Organization time zone. The organization_time filter uses it.",
        ["CurrentOrganization.DateFormat"] = "Organization date format.",
        ["CurrentOrganization.SmtpDefaultFromAddress"] = "Default sender address for email.",
        ["CurrentOrganization.SmtpDefaultFromName"] = "Default sender name for email.",
        ["CurrentOrganization.HomePageId"] = "Id of the page set as the home page.",
        ["CurrentOrganization.EmailAndPasswordIsEnabledForAdmin"] =
            "True when admins can sign in with email and password.",
        ["CurrentOrganization.EmailAndPasswordIsEnabledForUsers"] =
            "True when users can sign in with email and password.",
        ["CurrentUser.IsAuthenticated"] = "True when the visitor is signed in.",
        ["CurrentUser.IsAdmin"] = "True when the visitor is an admin.",
        ["CurrentUser.FullName"] = "First and last name of the signed-in user.",
        ["CurrentUser.EmailAddress"] = "Email address of the signed-in user.",
        ["CurrentUser.UserId"] = "Id of the signed-in user.",
        ["CurrentUser.Roles"] = "Roles the signed-in user holds.",
        ["CurrentUser.UserGroups"] = "User groups the signed-in user belongs to.",
        ["CurrentUser.AuthenticationScheme"] = "Sign-in method the user used.",
        ["CurrentUser.SsoId"] = "User id from the single sign-on provider.",
        ["CurrentUser.RemoteIpAddress"] = "IP address of the visitor.",
        ["Menu.MenuItems"] = "Top-level items of the menu.",
        ["Menu.IsMainMenu"] = "True for the menu set as the main menu.",
        ["MenuItem.Url"] = "Link target of the menu item.",
        ["MenuItem.OpenInNewTab"] = "True when the link should open in a new tab.",
        ["MenuItem.CssClassName"] = "CSS class set on the menu item.",
        ["MenuItem.IsFirstItem"] = "True for the first item at its level.",
        ["MenuItem.IsLastItem"] = "True for the last item at its level.",
        ["MenuItem.MenuItems"] = "Child items of the menu item.",
        ["ContentType.LabelPlural"] = "Plural label of the content type, e.g. Posts.",
        ["ContentType.LabelSingular"] = "Singular label of the content type, e.g. Post.",
        ["ContentType.DeveloperName"] = "Developer name of the content type.",
        ["Target.Items"] = "Content items on the current page of the list.",
        ["Target.TotalCount"] = "Number of items across every page.",
        ["Target.PageNumber"] = "Current page, starting at 1.",
        ["Target.PageSize"] = "Items per page.",
        ["Target.TotalPages"] = "Number of pages.",
        ["Target.Search"] = "Search text applied to the list.",
        ["Target.Filter"] = "Filter applied to the list.",
        ["Target.OrderBy"] = "Sort applied to the list.",
        ["Target.PreviousDisabledCss"] = "True on the first page. Use it to disable a previous link.",
        ["Target.NextDisabledCss"] = "True on the last page. Use it to disable a next link.",
        ["Target.FirstVisiblePageNumber"] = "First page number to show in pagination.",
        ["Target.LastVisiblePageNumber"] = "Last page number to show in pagination.",
        ["Target.PrimaryField"] = "Value of the content type's primary field.",
        ["Target.RoutePath"] = "Route path of the item or list, without PathBase.",
        ["Target.Template"] = "Developer name of the template rendering the item.",
        ["Target.CreationTime"] = "When the item was created.",
        ["Target.LastModificationTime"] = "When the item was last changed.",
        ["Target.ReturnUrl"] = "Where to send the visitor after the form succeeds.",
        ["Target.ValidationFailures"] = "Field errors from the last submit.",
        ["Target.SuccessMessage"] = "Message to show after a successful submit.",
        ["Target.RequestVerificationToken"] = "Antiforgery token for the form.",
        ["Target.LoginUrl"] = "Sign-in link for the recipient.",
        ["Target.ErrorMessage"] = "Error message to show the visitor.",
        ["Target.IsDevelopmentMode"] = "True when the site runs in development mode.",
    };

    private static readonly Dictionary<string, string> Examples = new()
    {
        ["Target.Items"] = """
            {% for item in Target.Items %}
              <a href="{{ PathBase }}/{{ item.RoutePath }}">{{ item.PrimaryField }}</a>
            {% endfor %}
            """,
        ["Menu.MenuItems"] = """
            {% for item in Menu.MenuItems %}
              <a href="{{ item.Url }}">{{ item.Label }}</a>
            {% endfor %}
            """,
        ["MenuItem.MenuItems"] = """
            {% for child in MenuItem.MenuItems %}
              <a href="{{ child.Url }}">{{ child.Label }}</a>
            {% endfor %}
            """,
        ["CurrentUser.IsAuthenticated"] = """
            {% if CurrentUser.IsAuthenticated %}
              Hello, {{ CurrentUser.FirstName }}
            {% endif %}
            """,
    };
}
