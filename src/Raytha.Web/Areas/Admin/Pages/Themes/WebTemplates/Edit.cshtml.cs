using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.MediaItems;
using Raytha.Application.Themes.MediaItems.Queries;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Pages.Themes.WebTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string themeId, string id)
    {
        var webTemplateResponse = await Mediator.Send(new GetWebTemplateById.Query { Id = id });

        var baseLayouts = await Mediator.Send(
            new GetWebTemplates.Query
            {
                ThemeId = themeId,
                PageSize = int.MaxValue,
                BaseLayoutsOnly = true,
            }
        );

        var childLayouts = GetChildren(
            baseLayouts.Result.Items.ToArray(),
            webTemplateResponse.Result
        );
        var lineage = childLayouts.Union(new List<WebTemplateDto>() { webTemplateResponse.Result });
        var excepted = baseLayouts
            .Result.Items.Select(p => p.DeveloperName)
            .Except(lineage.Select(p => p.DeveloperName));
        var baseLayoutsDictionary = baseLayouts
            .Result.Items.Where(p => excepted.Contains(p.DeveloperName))
            .ToDictionary(k => k.Id.ToString(), v => v.Label);

        var contentTypes = await Mediator.Send(new GetContentTypes.Query());
        var templateAccessChoiceItems = new List<WebTemplateAccessToModelDefinitionsViewModel>();
        foreach (var contentType in contentTypes.Result.Items)
        {
            templateAccessChoiceItems.Add(
                new WebTemplateAccessToModelDefinitionsViewModel
                {
                    Id = contentType.Id,
                    Key = contentType.LabelPlural,
                    Value = webTemplateResponse.Result.TemplateAccessToModelDefinitions.ContainsKey(
                        contentType.Id
                    ),
                }
            );
        }

        var templateVariableDictionary = GetInsertVariablesViewModel(
            webTemplateResponse.Result.DeveloperName,
            webTemplateResponse.Result.IsBuiltInTemplate,
            contentTypes.Result.Items
        );

        var mediaItemsResponse = await Mediator.Send(
            new GetMediaItemsByThemeId.Query { ThemeId = themeId }
        );

        var model = new FormModel
        {
            Id = webTemplateResponse.Result.Id,
            Content = webTemplateResponse.Result.Content,
            Label = webTemplateResponse.Result.Label,
            DeveloperName = webTemplateResponse.Result.DeveloperName,
            ParentTemplateId = webTemplateResponse.Result.ParentTemplateId,
            ParentTemplates = baseLayoutsDictionary,
            IsBaseLayout = webTemplateResponse.Result.IsBaseLayout,
            IsBuiltInTemplate = webTemplateResponse.Result.IsBuiltInTemplate,
            TemplateAccessToModelDefinitions = templateAccessChoiceItems.ToArray(),
            TemplateVariables = templateVariableDictionary,
            AllowAccessForNewContentTypes = webTemplateResponse
                .Result
                .AllowAccessForNewContentTypes,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            MaxFileSize = FileStorageProviderSettings.MaxFileSize,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            PathBase = CurrentOrganization.PathBase,
            ThemeId = themeId,
            MediaItems = mediaItemsResponse.Result,
        };

        if (
            WebTemplateExtensions.HasRenderBodyTag(webTemplateResponse.Result.Content)
            && !webTemplateResponse.Result.IsBaseLayout
        )
            SetWarningMessage(
                "{% renderbody %} is present and this template is not a base layout. This may result in a rendering error or crash if not handled properly."
            );

        return Page();
    }

    public Dictionary<
        string,
        IEnumerable<WebTemplatesInsertVariableListItemViewModel>
    > GetInsertVariablesViewModel(
        string templateName,
        bool isBuiltInTemplate,
        IEnumerable<ContentTypeDto> contentTypes
    )
    {
        var templateVariableDictionary =
            new Dictionary<string, IEnumerable<WebTemplatesInsertVariableListItemViewModel>>();
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
                templateVariableDictionary.Add(
                    $"{item.LabelSingular}",
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

    protected List<WebTemplateDto> GetChildren(WebTemplateDto[] list, WebTemplateDto startItem)
    {
        var result = new List<WebTemplateDto>();
        var children = list.Where(p => p.ParentTemplateId == startItem.Id).ToList();
        foreach (var child in children)
        {
            result.Add(child);
            result.AddRange(GetChildren(list, child));
        }
        return result;
    }

    public record FormModel
    {
        public string Id { get; set; }
        public string ThemeId { get; set; }

        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "This is a base layout that other templates can inherit from")]
        public bool IsBaseLayout { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Content")]
        public string Content { get; set; }

        [Display(Name = "Parent template")]
        public string ParentTemplateId { get; set; }

        [Display(Name = "The following content types can use this template")]
        public WebTemplateAccessToModelDefinitionsViewModel[] TemplateAccessToModelDefinitions { get; set; }

        [Display(Name = "This template can be accessed by newly created content types")]
        public bool AllowAccessForNewContentTypes { get; set; }

        public IEnumerable<MediaItemDto> MediaItems { get; set; }

        //helpers
        public long MaxFileSize { get; set; }
        public string AllowedMimeTypes { get; set; }
        public bool UseDirectUploadToCloud { get; set; }
        public string PathBase { get; set; }
        public Dictionary<string, string> ParentTemplates { get; set; }
        public bool IsBuiltInTemplate { get; set; }
        public Dictionary<
            string,
            IEnumerable<WebTemplatesInsertVariableListItemViewModel>
        > TemplateVariables { get; set; }
    }

    public record WebTemplatesInsertVariableListItemViewModel
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
