using CSharpVitamins;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Domain.Common;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Application.ContentItems;

public class FieldValueConverter
{
    private readonly ICurrentOrganization _currentOrganization;
    public FieldValueConverter(ICurrentOrganization currentOrganization)
    {
        _currentOrganization = currentOrganization;
    }

    public Dictionary<string, string> MapToListItemValues(ContentItemDto item, string templateLabel)
    {
        var viewModel = new Dictionary<string, string>
        {
            //Built in
            { BuiltInContentTypeField.Id, item.Id },
            { "CreatorUser", item.CreatorUser != null ? item.CreatorUser.FullName : "N/A" },
            { "LastModifierUser", item.LastModifierUser != null ? item.LastModifierUser.FullName : "N/A" },
            { BuiltInContentTypeField.CreationTime, _currentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(item.CreationTime) },
            { BuiltInContentTypeField.LastModificationTime, _currentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(item.LastModificationTime) },
            { BuiltInContentTypeField.IsPublished, item.IsPublished.YesOrNo() },
            { BuiltInContentTypeField.IsDraft, item.IsDraft.YesOrNo() },
            { BuiltInContentTypeField.PrimaryField, item.PrimaryField },
            { "Template", templateLabel },
        };

        //Content type fields
        foreach (var field in item.PublishedContent as Dictionary<string, dynamic>)
        {
            if (field.Value is DateTimeFieldValue dateTimeFieldValue)
            {
                viewModel.Add(field.Key, _currentOrganization.TimeZoneConverter.ToDateFormat(dateTimeFieldValue.Value));
            }
            else if (field.Value is GuidFieldValue guidFieldValue)
            {
                if (guidFieldValue.HasValue)
                    viewModel.Add(field.Key, (ShortGuid)guidFieldValue.Value);
                else
                    viewModel.Add(field.Key, string.Empty);
            }
            else if (field.Value is StringFieldValue)
            {
                viewModel.Add(field.Key, ((string)field.Value).StripHtml().Truncate(40));
            }
            else if (field.Value is IBaseEntity)
            {
                viewModel.Add(field.Key, field.Value.PrimaryField);
            }
            else
            {
                viewModel.Add(field.Key, field.Value.ToString());
            }
        }

        return viewModel;
    }

    public string MapValueForChoiceField(BaseFieldType fieldType, dynamic content, ContentTypeFieldDto contentTypeField, ContentTypeFieldChoice contentTypeFieldChoice)
    {
        string value = "false";
        if (fieldType.DeveloperName == BaseFieldType.MultipleSelect)
        {
            if (content.ContainsKey(contentTypeField.DeveloperName) && content[contentTypeField.DeveloperName].HasValue)
            {
                var asArray = content[contentTypeField.DeveloperName].Value as IList<string>;
                bool tempValue = asArray.Contains(contentTypeFieldChoice.DeveloperName);
                value = tempValue.ToString();
            }
        }
        else
        {
            value = contentTypeFieldChoice.DeveloperName;
        }
        return value;
    }

    public string MapValueForField(BaseFieldType fieldType, dynamic content, ContentTypeFieldDto contentTypeField)
    {
        string value = string.Empty;
        if (fieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
        {
            ShortGuid shortGuid = ShortGuid.Empty;
            if (content.ContainsKey(contentTypeField.DeveloperName) && content[contentTypeField.DeveloperName] != null)
            {
                var successfullyParsed = ShortGuid.TryParse(content[contentTypeField.DeveloperName].ToString(), out shortGuid) || (content[contentTypeField.DeveloperName] is ContentItemDto && ShortGuid.TryParse(content[contentTypeField.DeveloperName].Id.ToString(), out shortGuid));
                if (successfullyParsed)
                {
                    value = shortGuid;
                }
            }
        }
        else if (fieldType.DeveloperName == BaseFieldType.Checkbox)
        {
            if (content.ContainsKey(contentTypeField.DeveloperName))
            {
                bool tempValue = content[contentTypeField.DeveloperName].HasValue && content[contentTypeField.DeveloperName].Value;
                value = tempValue.ToString();
            }
            else
            {
                value = "false";
            }
        }
        else if (content.ContainsKey(contentTypeField.DeveloperName))
        {
            value = content[contentTypeField.DeveloperName].ToString();
        }
        return value;
    }

    public string MapRelatedContentItemValueForField(BaseFieldType fieldType, dynamic content, ContentTypeFieldDto contentTypeField)
    {
        string value = string.Empty;
        if (fieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
        {
            if (content.ContainsKey(contentTypeField.DeveloperName) && content[contentTypeField.DeveloperName] is ContentItemDto && content[contentTypeField.DeveloperName] != null)
            {
                value = content[contentTypeField.DeveloperName].PrimaryField;
            }
        }
        return value;
    }
}
