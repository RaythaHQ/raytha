using System.Text.Json;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.MediaItems.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
public class Edit : BaseHasFavoriteViewsPageModel, ISubActionViewModel
{
    public string Id { get; set; }
    ViewDto ISubActionViewModel.CurrentView => base.CurrentView;
    private FieldValueConverter _fieldValueConverter;

    protected FieldValueConverter FieldValueConverter =>
        _fieldValueConverter ??=
            HttpContext.RequestServices.GetRequiredService<FieldValueConverter>();
    public string WebsiteUrl { get; set; }
    public Dictionary<string, string> ContentTypeFields { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetContentItemById.Query { Id = id });

        var imageMediaItemsResponse = await Mediator.Send(
            new GetMediaItems.Query { ContentType = "image" }
        );

        var videoMediaItemsResponse = await Mediator.Send(
            new GetMediaItems.Query { ContentType = "video" }
        );

        Form = new FormModel
        {
            Id = response.Result.Id,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            MaxFileSize = FileStorageProviderSettings.MaxFileSize,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            PathBase = CurrentOrganization.PathBase,
            ImageMediaItemsJson = JsonSerializer.Serialize(
                imageMediaItemsResponse.Result.Items.Select(mi => new
                {
                    fileName = mi.FileName,
                    url = RelativeUrlBuilder.MediaRedirectToFileUrl(mi.ObjectKey),
                })
            ),
            VideoMediaItemsJson = JsonSerializer.Serialize(
                videoMediaItemsResponse.Result.Items.Select(mi => new
                {
                    fileName = mi.FileName,
                    url = RelativeUrlBuilder.MediaRedirectToFileUrl(mi.ObjectKey),
                })
            ),
            FieldValues = CurrentView
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
                            Value = FieldValueConverter.MapValueForChoiceField(
                                p.FieldType,
                                response.Result.DraftContent,
                                p,
                                b
                            ),
                        })
                        .ToArray(),
                    FieldType = p.FieldType,
                    IsRequired = p.IsRequired,
                    Description = p.Description,
                    Value = FieldValueConverter.MapValueForField(
                        p.FieldType,
                        response.Result.DraftContent,
                        p
                    ),
                    RelatedContentItemPrimaryField =
                        FieldValueConverter.MapRelatedContentItemValueForField(
                            p.FieldType,
                            response.Result.DraftContent,
                            p
                        ),
                    RelatedContentTypeId = p.RelatedContentTypeId,
                })
                .ToArray(),
        };

        Id = id;

        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var mappedFieldValuesFromModel = MapFromFieldValueModel(Form.FieldValues);
        var input = new EditContentItem.Command
        {
            SaveAsDraft = Form.SaveAsDraft,
            Id = Form.Id,
            Content = mappedFieldValuesFromModel,
        };
        var editResponse = await Mediator.Send(input);

        if (editResponse.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} was created successfully.");
            return RedirectToAction(
                "Edit",
                new
                {
                    contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                    id = editResponse.Result,
                }
            );
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this page. See the error below.",
                editResponse.GetErrors()
            );

            var response = await Mediator.Send(new GetContentItemById.Query { Id = id });
            Form = new FormModel
            {
                Id = Form.Id,
                AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
                MaxFileSize = FileStorageProviderSettings.MaxFileSize,
                UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
                PathBase = CurrentOrganization.PathBase,
                FieldValues = CurrentView
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
                                Value = FieldValueConverter.MapValueForChoiceField(
                                    p.FieldType,
                                    response.Result.DraftContent,
                                    p,
                                    b
                                ),
                            })
                            .ToArray(),
                        FieldType = p.FieldType,
                        IsRequired = p.IsRequired,
                        Description = p.Description,
                        Value = FieldValueConverter.MapValueForField(
                            p.FieldType,
                            response.Result.DraftContent,
                            p
                        ),
                        RelatedContentItemPrimaryField =
                            FieldValueConverter.MapRelatedContentItemValueForField(
                                p.FieldType,
                                response.Result.DraftContent,
                                p
                            ),
                        RelatedContentTypeId = p.RelatedContentTypeId,
                    })
                    .ToArray(),
            };
        }

        Id = id;

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
                var selectedChoices = fieldValue
                    .AvailableChoices.Where(p => p.Value == "true")
                    .Select(p => p.DeveloperName)
                    .ToArray();
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

    public record FormModel
    {
        public string Id { get; set; }
        public bool SaveAsDraft { get; set; }
        public FieldValueViewModel[] FieldValues { get; set; }
        public long MaxFileSize { get; set; }
        public string AllowedMimeTypes { get; set; }
        public bool UseDirectUploadToCloud { get; set; }
        public string PathBase { get; set; }
        public string ImageMediaItemsJson { get; set; }
        public string VideoMediaItemsJson { get; set; }

        public bool IsDraft { get; set; }
        public bool IsPublished { get; set; }
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
