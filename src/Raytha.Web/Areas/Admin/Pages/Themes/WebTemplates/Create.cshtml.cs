using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.MediaItems;
using Raytha.Application.Themes.MediaItems.Queries;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Pages.Themes.WebTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public string UploadAssetsUrl =>
        Url.Page("/Api/MediaItems/UploadForTheme", new { themeId = Form.ThemeId });

    public async Task<IActionResult> OnGet(string themeId)
    {
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Themes", RouteName = RouteNames.Themes.Index, IsActive = false, Icon = SidebarIcons.Themes },
            new BreadcrumbNode { Label = "Web Templates", RouteName = RouteNames.Themes.WebTemplates.Index, IsActive = false },
            new BreadcrumbNode { Label = "Create", RouteName = RouteNames.Themes.WebTemplates.Create, IsActive = true }
        );

        var webTemplatesResponse = await Mediator.Send(
            new GetWebTemplates.Query
            {
                ThemeId = themeId,
                PageSize = int.MaxValue,
                BaseLayoutsOnly = true,
            }
        );

        var contentTypesResponse = await Mediator.Send(
            new GetContentTypes.Query { PageSize = int.MaxValue }
        );

        var mediaItemsResponse = await Mediator.Send(
            new GetMediaItemsByThemeId.Query { ThemeId = themeId }
        );

        var templateAccessList = contentTypesResponse.Result.Items.Select(
            p => new WebTemplateAccessToModelDefinitionsViewModel
            {
                Id = p.Id,
                Key = p.LabelPlural,
                Value = true,
            }
        );

        var templateVariableDictionary = GetInsertVariablesViewModel(
            string.Empty,
            false,
            contentTypesResponse.Result.Items
        );

        Form = new FormModel
        {
            ThemeId = themeId,
            ParentTemplates = webTemplatesResponse.Result.Items,
            TemplateAccessToModelDefinitions = templateAccessList.ToArray(),
            TemplateVariables = templateVariableDictionary,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            MaxFileSize = FileStorageProviderSettings.MaxFileSize,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            PathBase = CurrentOrganization.PathBase,
            MediaItems = mediaItemsResponse.Result,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(string themeId)
    {
        var input = new CreateWebTemplate.Command
        {
            ThemeId = themeId,
            Label = Form.Label,
            Content = Form.Content,
            ParentTemplateId = Form.ParentTemplateId,
            IsBaseLayout = Form.IsBaseLayout,
            DeveloperName = Form.DeveloperName,
            TemplateAccessToModelDefinitions = Form
                .TemplateAccessToModelDefinitions.Where(p => p.Value)
                .Select(p => (ShortGuid)p.Id),
            AllowAccessForNewContentTypes = Form.AllowAccessForNewContentTypes,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage(
                RouteNames.Themes.WebTemplates.Edit,
                new { themeId, id = response.Result }
            );
        }
        else
        {
            var baseLayouts = await Mediator.Send(
                new GetWebTemplates.Query
                {
                    ThemeId = themeId,
                    PageSize = int.MaxValue,
                    BaseLayoutsOnly = true,
                }
            );

            Form.ParentTemplates = baseLayouts.Result.Items;

            var contentTypes = await Mediator.Send(
                new GetContentTypes.Query { PageSize = int.MaxValue }
            );
            var templateVariableDictionary = GetInsertVariablesViewModel(
                Form.DeveloperName,
                false,
                contentTypes.Result.Items
            );
            Form.TemplateVariables = templateVariableDictionary;

            var mediaItemsResponse = await Mediator.Send(
                new GetMediaItemsByThemeId.Query { ThemeId = themeId }
            );

            Form.MediaItems = mediaItemsResponse.Result;

            Form.ThemeId = themeId;
            Form.AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes;
            Form.MaxFileSize = FileStorageProviderSettings.MaxFileSize;
            Form.UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud;
            Form.PathBase = CurrentOrganization.PathBase;

            SetErrorMessage(
                "There was an error attempting to update this template. See the error below.",
                response.GetErrors()
            );

            return Page();
        }
    }

    public Dictionary<
        string,
        IEnumerable<IWebTemplatesInsertVariableListItemViewModel>
    > GetInsertVariablesViewModel(
        string templateName,
        bool isBuiltInTemplate,
        IEnumerable<ContentTypeDto> contentTypes
    )
    {
        var templateVariableDictionary =
            new Dictionary<string, IEnumerable<IWebTemplatesInsertVariableListItemViewModel>>();
        var requestVariables = InsertVariableTemplateFactory
            .Request.TemplateInfo.GetTemplateVariables()
            .Select(p => new WebTemplatesInsertVariableListItemViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value,
            });

        var currentOrgVariables = InsertVariableTemplateFactory
            .CurrentOrganization.TemplateInfo.GetTemplateVariables()
            .Select(p => new WebTemplatesInsertVariableListItemViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value,
            });

        var currentUserVariables = InsertVariableTemplateFactory
            .CurrentUser.TemplateInfo.GetTemplateVariables()
            .Select(p => new WebTemplatesInsertVariableListItemViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value,
            });

        var navigationMenuVariables = InsertVariableTemplateFactory
            .NavigationMenu.TemplateInfo.GetTemplateVariables()
            .Select(p => new WebTemplatesInsertVariableListItemViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value,
            });

        var navigationMenuItemVariables = InsertVariableTemplateFactory
            .NavigationMenuItem.TemplateInfo.GetTemplateVariables()
            .Select(p => new WebTemplatesInsertVariableListItemViewModel
            {
                DeveloperName = p.Key,
                TemplateVariable = p.Value,
            });

        templateVariableDictionary.Add(
            InsertVariableTemplateFactory.Request.VariableCategoryName,
            requestVariables
        );
        templateVariableDictionary.Add(
            InsertVariableTemplateFactory.CurrentOrganization.VariableCategoryName,
            currentOrgVariables
        );
        templateVariableDictionary.Add(
            InsertVariableTemplateFactory.CurrentUser.VariableCategoryName,
            currentUserVariables
        );
        templateVariableDictionary.Add(
            InsertVariableTemplateFactory.NavigationMenu.VariableCategoryName,
            navigationMenuVariables
        );
        templateVariableDictionary.Add(
            InsertVariableTemplateFactory.NavigationMenuItem.VariableCategoryName,
            navigationMenuItemVariables
        );

        if (ShowContentVariablesForTemplate(templateName) || !isBuiltInTemplate)
        {
            var contentTypeVariables = InsertVariableTemplateFactory
                .ContentType.TemplateInfo.GetTemplateVariables()
                .Select(p => new WebTemplatesInsertVariableListItemViewModel
                {
                    DeveloperName = p.Key,
                    TemplateVariable = p.Value,
                });

            var builtInContentItemVariables = InsertVariableTemplateFactory
                .ContentItem.TemplateInfo.GetTemplateVariables()
                .Select(p => new WebTemplatesInsertVariableListItemViewModel
                {
                    DeveloperName = p.Key,
                    TemplateVariable = p.Value,
                });

            var listResultVariables = InsertVariableTemplateFactory
                .ContentItemListResult.TemplateInfo.GetTemplateVariables()
                .Select(p => new WebTemplatesInsertVariableListItemViewModel
                {
                    DeveloperName = p.Key,
                    TemplateVariable = p.Value,
                });

            templateVariableDictionary.Add(
                InsertVariableTemplateFactory.ContentType.VariableCategoryName,
                contentTypeVariables
            );
            templateVariableDictionary.Add(
                $"{InsertVariableTemplateFactory.ContentItemListResult.VariableCategoryName} (list result)",
                listResultVariables
            );
            templateVariableDictionary.Add(
                $"{InsertVariableTemplateFactory.ContentItem.VariableCategoryName} (single item)",
                builtInContentItemVariables
            );

            foreach (var item in contentTypes)
            {
                var allCustomVariables = item
                    .ContentTypeFields.Select(p => new WebTemplatesInsertVariableListItemViewModel
                    {
                        DeveloperName = $"{p.DeveloperName}{RenderValueProperty(p.FieldType)}",
                        TemplateVariable =
                            $"{InsertVariableTemplateFactory.ContentItem.VariableCategoryName}.PublishedContent.{p.DeveloperName}{RenderValueProperty(p.FieldType)}",
                    })
                    .ToList();
                allCustomVariables.AddRange(
                    item.ContentTypeFields.Where(p =>
                            p.FieldType.DeveloperName
                            != BaseFieldType.OneToOneRelationship.DeveloperName
                        )
                        .Select(p => new WebTemplatesInsertVariableListItemViewModel
                        {
                            DeveloperName = $"{p.DeveloperName}.Value",
                            TemplateVariable =
                                $"{InsertVariableTemplateFactory.ContentItem.VariableCategoryName}.PublishedContent.{p.DeveloperName}.Value",
                        })
                );
                
                // Ensure unique key by using DeveloperName as fallback if LabelSingular already exists
                var key = item.LabelSingular;
                if (templateVariableDictionary.ContainsKey(key))
                {
                    key = $"{item.LabelSingular} ({item.DeveloperName})";
                }
                
                templateVariableDictionary.Add(
                    key,
                    allCustomVariables.OrderBy(p => p.DeveloperName)
                );
            }
        }
        else
        {
            var webTemplateVariables = InsertVariableTemplateFactory
                .From(templateName)
                .TemplateInfo.GetTemplateVariables()
                .Select(p => new WebTemplatesInsertVariableListItemViewModel
                {
                    DeveloperName = p.Key,
                    TemplateVariable = p.Value,
                });
            templateVariableDictionary.Add(
                InsertVariableTemplateFactory.From(templateName).VariableCategoryName,
                webTemplateVariables
            );
        }

        return templateVariableDictionary;
    }

    public string RenderValueProperty(BaseFieldType fieldType)
    {
        return fieldType.DeveloperName != BaseFieldType.OneToOneRelationship.DeveloperName
            ? $".Text"
            : string.Empty;
    }

    public bool ShowContentVariablesForTemplate(string templateName)
    {
        return BuiltInWebTemplate._Layout.DeveloperName == templateName;
    }

    public record FormModel
    {
        public string ThemeId { get; set; }

        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Content")]
        public string Content { get; set; }

        [Display(Name = "Parent template")]
        public string ParentTemplateId { get; set; }

        [Display(Name = "The following content types can use this template")]
        public WebTemplateAccessToModelDefinitionsViewModel[] TemplateAccessToModelDefinitions { get; set; }

        [Display(Name = "This is a base layout that other templates can inherit from")]
        public bool IsBaseLayout { get; set; } = false;

        [Display(Name = "This template can be accessed by newly created content types")]
        public bool AllowAccessForNewContentTypes { get; set; } = true;

        public IEnumerable<MediaItemDto> MediaItems { get; set; }

        //helpers
        public long MaxFileSize { get; set; }
        public string AllowedMimeTypes { get; set; }
        public bool UseDirectUploadToCloud { get; set; }
        public string PathBase { get; set; }
        public IEnumerable<WebTemplateDto> ParentTemplates { get; set; }
        public Dictionary<
            string,
            IEnumerable<IWebTemplatesInsertVariableListItemViewModel>
        > TemplateVariables { get; set; }
    }

    public record WebTemplatesInsertVariableListItemViewModel
        : IWebTemplatesInsertVariableListItemViewModel
    {
        public string DeveloperName { get; set; }
        public string TemplateVariable { get; set; }
    }

    public record WebTemplateAccessToModelDefinitionsViewModel
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public bool Value { get; set; }
    }
}
