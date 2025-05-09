﻿using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using System.Data;
using System.Text.Json;

namespace Raytha.Infrastructure.JsonQueryEngine;

internal abstract class AbstractRaythaDbJsonQueryEngine
{
    protected ContentType ContentType;
    protected List<ContentTypeField> OneToOneRelationshipFields => ContentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship && p.RelatedContentTypeId.HasValue).ToList();
    protected string PrimaryFieldDeveloperName => ContentType.ContentTypeFields.First(p => p.Id == ContentType.PrimaryFieldId).DeveloperName;

    public abstract ContentItem FirstOrDefault(Guid entityId);
    public abstract IEnumerable<ContentItem> QueryContentItems(Guid contentTypeId, string[] searchOnColumns, string search, string[] filters, int pageSize, int pageNumber, string orderBy, IDbTransaction transaction = null);
    public abstract IEnumerable<ContentItem> QueryAllContentItemsAsTransaction(Guid contentTypeId, string[] searchOnColumns, string search, string[] filters, string orderBy);
    public abstract int CountContentItems(Guid contentTypeId, string[] searchOnColumns, string search, string[] filters, IDbTransaction transaction = null);
    protected abstract SqlQueryBuilder PrepareContentItemsDataSelect(SqlQueryBuilder sqlBuilder);
    protected abstract SqlQueryBuilder PrepareFrom(SqlQueryBuilder sqlBuilder);
    protected abstract SqlQueryBuilder PrepareSearch(SqlQueryBuilder sqlBuilder, string search, string[] searchOnColumns = null);
    protected abstract SqlQueryBuilder PrepareOrderBy(SqlQueryBuilder sqlBuilder, string orderBy); 

    protected ContentItem MapQueryItemToContentItem(IDictionary<string, object> resultFromQuery)
    {
        List<ContentItem> relatedContentItems = new List<ContentItem>();
        foreach (var item in OneToOneRelationshipFields)
        {
            int index = OneToOneRelationshipFields.IndexOf(item);
            var relatedCreatorUser = ToStatic<User>(resultFromQuery.FilterAndTrimKeys($"{RawSqlColumn.RELATED_CREATED_BY_COLUMN_NAME}_{index}_"));
            var relatedModifierUser = ToStatic<User>(resultFromQuery.FilterAndTrimKeys($"{RawSqlColumn.RELATED_MODIFIED_BY_COLUMN_NAME}_{index}_"));
            var relatedRoute = ToStatic<Route>(resultFromQuery.FilterAndTrimKeys($"{RawSqlColumn.RELATED_ROUTE_COLUMN_NAME}_{index}_"));
            var relatedItem = ToStatic<ContentItem>(resultFromQuery.FilterAndTrimKeys($"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}_"));
            if (relatedItem != null && relatedItem.Id != Guid.Empty)
            {
                relatedItem.CreatorUser = relatedCreatorUser;
                relatedItem.LastModifierUser = relatedModifierUser;
                relatedItem.Route = relatedRoute;
                relatedItem.PublishedContent = ConvertJsonContentToDynamic(relatedItem._PublishedContent, item.RelatedContentType);
                relatedItem.DraftContent = ConvertJsonContentToDynamic(relatedItem._DraftContent, item.RelatedContentType);
                string relatedPrimaryFieldDeveloperName = item.RelatedContentType.ContentTypeFields.First(p => p.Id == item.RelatedContentType.PrimaryFieldId).DeveloperName;
                if (relatedItem.PublishedContent.ContainsKey(relatedPrimaryFieldDeveloperName))
                    relatedItem.PrimaryField = relatedItem.PublishedContent[relatedPrimaryFieldDeveloperName];
                relatedContentItems.Add(relatedItem);
            }
        }

        var contentItem = ToStatic<ContentItem>(resultFromQuery.FilterAndTrimKeys($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}_"));
        var creatorUser = ToStatic<User>(resultFromQuery.FilterAndTrimKeys($"{RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}_"));
        var modifierUser = ToStatic<User>(resultFromQuery.FilterAndTrimKeys($"{RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}_"));
        var route = ToStatic<Route>(resultFromQuery.FilterAndTrimKeys($"{RawSqlColumn.ROUTE_COLUMN_NAME}_"));

        contentItem.PublishedContent = ConvertJsonContentToDynamic(contentItem._PublishedContent, ContentType, relatedContentItems);
        contentItem.DraftContent = ConvertJsonContentToDynamic(contentItem._DraftContent, ContentType, relatedContentItems);

        if (contentItem.PublishedContent.ContainsKey(PrimaryFieldDeveloperName))
            contentItem.PrimaryField = contentItem.PublishedContent[PrimaryFieldDeveloperName];

        contentItem.CreatorUser = creatorUser;
        contentItem.LastModifierUser = modifierUser;
        contentItem.ContentType = ContentType;
        contentItem.ContentTypeId = ContentType.Id;
        contentItem.Route = route;

        return contentItem;
    }

    protected T ToStatic<T>(IEnumerable<KeyValuePair<string, object>> properties)
    {
        if (properties == null || !properties.Any())
            return default(T);

        var entity = Activator.CreateInstance<T>();

        foreach (var entry in properties)
        {
            var propertyInfo = entity.GetType().GetProperty(entry.Key);
            if (propertyInfo != null)
                propertyInfo.SetValue(entity, entry.Value, null);
        }

        return entity;
    }

    protected dynamic ConvertJsonContentToDynamic(string contentAsJson, ContentType contentType, IEnumerable<ContentItem> relatedContentItems = null)
    {
        Dictionary<string, dynamic> contentAsAssembled = new Dictionary<string, object>();
        if (string.IsNullOrEmpty(contentAsJson))
            return contentAsAssembled;

        Dictionary<string, dynamic> contentAsRawDict = JsonSerializer.Deserialize<Dictionary<string, object>>(contentAsJson);

        if (contentAsRawDict == null)
            return contentAsAssembled;

        foreach (var keyValue in contentAsRawDict)
        {
            var contentTypeField = contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == keyValue.Key);
            if (contentTypeField == null)
                continue;

            if (contentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship && relatedContentItems != null)
            {
                var relatedItemFieldValue = contentTypeField.FieldType.FieldValueFrom(keyValue.Value);
                var relatedItem = relatedContentItems.FirstOrDefault(p => p.Id == relatedItemFieldValue.Value);
                if (relatedItem != null)
                {
                    contentAsAssembled.Add(keyValue.Key, ContentItemDto.GetProjection(relatedItem));
                }
                else
                {
                    contentAsAssembled.Add(keyValue.Key, string.Empty);
                }
            }
            else
            {
                contentAsAssembled.Add(keyValue.Key, contentTypeField.FieldType.FieldValueFrom(keyValue.Value));
            }        
        }

        return contentAsAssembled;
    }
}
