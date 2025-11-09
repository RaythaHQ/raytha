using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Application.MediaItems.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Create : BaseHasFavoriteViewsPageModel
{
    private FieldValueConverter _fieldValueConverter;

    protected FieldValueConverter FieldValueConverter =>
        _fieldValueConverter ??=
            HttpContext.RequestServices.GetRequiredService<FieldValueConverter>();
    public string BackToListUrl { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string backToListUrl = "")
    {
        var webTemplates = await GetWebTemplatesAsync();
        var (imageJson, videoJson) = await GetMediaItemsJsonAsync();
        var fieldValues = BuildFieldValuesForCreate(CurrentView.ContentType.ContentTypeFields);

        Form = new FormModel
        {
            AvailableTemplates = webTemplates,
            FieldValues = fieldValues,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            MaxFileSize = FileStorageProviderSettings.MaxFileSize,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            PathBase = CurrentOrganization.PathBase,
            ImageMediaItemsJson = imageJson,
            VideoMediaItemsJson = videoJson,
        };

        BackToListUrl = backToListUrl;
        return Page();
    }

    public async Task<IActionResult> OnPost(string backToListUrl = "")
    {
        var mappedFieldValuesFromModel = MapFromFieldValueModel(Form.FieldValues);
        var input = new CreateContentItem.Command
        {
            SaveAsDraft = Form.SaveAsDraft,
            TemplateId = Form.TemplateId,
            ContentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
            Content = mappedFieldValuesFromModel,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} was created successfully.");
            return RedirectToPage(
                "Edit",
                new
                {
                    contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                    id = response.Result,
                    backToListUrl,
                }
            );
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this page. See the error below.",
                response.GetErrors()
            );

            await RepopulateFormOnError();
        }

        BackToListUrl = backToListUrl;
        return Page();
    }

    private dynamic MapFromFieldValueModel(FieldValueViewModel[] fieldValues)
    {
        var mappedFieldValues = new Dictionary<string, dynamic>();

        foreach (var fieldValue in fieldValues)
        {
            var contentTypeField = CurrentView.ContentType.ContentTypeFields.First(p =>
                p.DeveloperName == fieldValue.DeveloperName.ToDeveloperName()
            );
            if (contentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
            {
                Guid guid = (ShortGuid)fieldValue.Value;
                mappedFieldValues.Add(fieldValue.DeveloperName.ToDeveloperName(), guid);
            }
            else if (contentTypeField.FieldType.DeveloperName == BaseFieldType.MultipleSelect)
            {
                var selectedChoices =
                    fieldValue
                        .AvailableChoices?.Where(p => p.Value == "true")
                        .Select(p => p.DeveloperName)
                        .ToArray() ?? Array.Empty<string>();
                mappedFieldValues.Add(
                    fieldValue.DeveloperName,
                    contentTypeField.FieldType.FieldValueFrom(selectedChoices).Value
                );
            }
            else
            {
                mappedFieldValues.Add(fieldValue.DeveloperName, fieldValue.Value);
            }
        }

        return mappedFieldValues;
    }

    private async Task<Dictionary<ShortGuid, string>> GetWebTemplatesAsync()
    {
        var webTemplates = await Mediator.Send(
            new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue,
            }
        );
        return webTemplates.Result?.Items.ToDictionary(p => p.Id, p => p.Label)
            ?? new Dictionary<ShortGuid, string>();
    }

    private async Task<(string ImageJson, string VideoJson)> GetMediaItemsJsonAsync()
    {
        var imageMediaItemsResponse = await Mediator.Send(
            new GetMediaItems.Query { ContentType = "image" }
        );

        var videoMediaItemsResponse = await Mediator.Send(
            new GetMediaItems.Query { ContentType = "video" }
        );

        var imageJson = JsonSerializer.Serialize(
            imageMediaItemsResponse.Result.Items.Select(mi => new
            {
                fileName = mi.FileName,
                url = RelativeUrlBuilder.MediaRedirectToFileUrl(mi.ObjectKey),
            })
        );

        var videoJson = JsonSerializer.Serialize(
            videoMediaItemsResponse.Result.Items.Select(mi => new
            {
                fileName = mi.FileName,
                url = RelativeUrlBuilder.MediaRedirectToFileUrl(mi.ObjectKey),
            })
        );

        return (imageJson, videoJson);
    }

    private FieldValueViewModel[] BuildFieldValuesForCreate(
        IEnumerable<ContentTypeFieldDto> contentTypeFields
    )
    {
        return contentTypeFields
            .Select(p => new FieldValueViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                AvailableChoices = p
                    .Choices?.Select(b => new FieldValueChoiceItemViewModel
                    {
                        Label = b.Label,
                        DeveloperName = b.DeveloperName,
                        Disabled = b.Disabled,
                        Value =
                            p.FieldType.DeveloperName == BaseFieldType.MultipleSelect
                                ? "false"
                                : b.DeveloperName,
                    })
                    .ToArray(),
                FieldType = p.FieldType,
                IsRequired = p.IsRequired,
                Description = p.Description,
                RelatedContentTypeId = p.RelatedContentTypeId,
            })
            .ToArray();
    }

    private async Task RepopulateFormOnError()
    {
        var webTemplates = await GetWebTemplatesAsync();

        var fieldValues = CurrentView
            .ContentType.ContentTypeFields.Select(p => new FieldValueViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                AvailableChoices = p
                    .Choices?.Select(b => new FieldValueChoiceItemViewModel
                    {
                        Label = b.Label,
                        DeveloperName = b.DeveloperName,
                        Disabled = b.Disabled,
                        Value =
                            p.FieldType.DeveloperName == BaseFieldType.MultipleSelect
                                ? (Form
                                    .FieldValues.First(c =>
                                        c.DeveloperName.ToDeveloperName() == p.DeveloperName
                                    )
                                    .AvailableChoices?.Where(a => a.Value == "true")
                                    .Select(z => z.DeveloperName.ToDeveloperName())
                                    .Contains(b.DeveloperName) ?? false)
                                    .ToString()
                                : b.DeveloperName,
                    })
                    .ToArray(),
                FieldType = p.FieldType,
                IsRequired = p.IsRequired,
                Description = p.Description,
                RelatedContentItemPrimaryField = Form
                    .FieldValues.First(c => c.DeveloperName.ToDeveloperName() == p.DeveloperName)
                    .RelatedContentItemPrimaryField,
                RelatedContentTypeId = p.RelatedContentTypeId,
                Value = Form
                    .FieldValues.First(c => c.DeveloperName.ToDeveloperName() == p.DeveloperName)
                    .Value,
            })
            .ToArray();

        Form.AvailableTemplates = webTemplates;
        Form.FieldValues = fieldValues;
    }

    public record FormModel
    {
        [Display(Name = "Template")]
        public string TemplateId { get; set; }
        public bool SaveAsDraft { get; set; }
        public FieldValueViewModel[] FieldValues { get; set; }
        public long MaxFileSize { get; set; }
        public string AllowedMimeTypes { get; set; }
        public bool UseDirectUploadToCloud { get; set; }
        public string PathBase { get; set; }
        public string ImageMediaItemsJson { get; set; }
        public string VideoMediaItemsJson { get; set; }

        //helpers
        public Dictionary<ShortGuid, string> AvailableTemplates { get; set; }
    }

    public class FieldValueViewModel
    {
        public string FieldType { get; set; }
        public string Label { get; set; }
        public string DeveloperName { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public string Value { get; set; }
        public FieldValueChoiceItemViewModel[] AvailableChoices { get; set; }
        public string RelatedContentTypeId { get; set; }
        public string RelatedContentItemPrimaryField { get; set; }

        //helpers
        public string AsteriskCssIfRequired => IsRequired ? "raytha-required" : string.Empty;
    }

    public class FieldValueChoiceItemViewModel
    {
        public string Label { get; set; }
        public string DeveloperName { get; set; }
        public bool Disabled { get; set; }
        public string Value { get; set; }
    }
}
