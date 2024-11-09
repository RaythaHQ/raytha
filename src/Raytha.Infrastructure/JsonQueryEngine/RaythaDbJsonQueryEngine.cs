using CSharpVitamins;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using System.Data;
using System.Text.Json;

namespace Raytha.Infrastructure.JsonQueryEngine;

public class RaythaDbJsonQueryEngine : IRaythaDbJsonQueryEngine
{
    public readonly IRaythaDbContext _entityFramework;
    public readonly IDbConnection _db;
    public readonly ICurrentOrganization _currentOrganization;

    private ContentType ContentType;
    private List<ContentTypeField> OneToOneRelationshipFields => ContentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship && p.RelatedContentTypeId.HasValue).ToList();
    private string PrimaryFieldDeveloperName => ContentType.ContentTypeFields.First(p => p.Id == ContentType.PrimaryFieldId).DeveloperName;

    public RaythaDbJsonQueryEngine(IRaythaDbContext entityFramework, IDbConnection db, ICurrentOrganization currentOrganization)
    {
        _entityFramework = entityFramework;
        _db = db;
        _currentOrganization = currentOrganization;
    }

    public ContentItem FirstOrDefault(Guid entityId)
    {
        //Check if it exists at all, and if so, get the ContentType
        var entity = _entityFramework.ContentItems
            .Include(p => p.ContentType)
            .ThenInclude(p => p.ContentTypeFields)
            .ThenInclude(p => p.RelatedContentType)
            .ThenInclude(p => p.ContentTypeFields)
            .FirstOrDefault(p => p.Id == entityId);

        if (entity == null)
            return null;

        ContentType = entity.ContentType;

        //Assemble the SQL select and from statements
        var sqlBuilder = PrepareFrom(PrepareContentItemsDataSelect(new SqlQueryBuilder()));

        //Attach the where clause to get just this specific item
        sqlBuilder.AndWhere($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.Id.Name} = '{entityId}'");

        var sqlStatement = sqlBuilder.Build();

        IDictionary<string, object> resultFromQuery = _db.QuerySingle(sqlStatement);
        if (resultFromQuery == null)
            return null;

        var contentItem = MapQueryItemToContentItem(resultFromQuery);
        return contentItem;
    }

    public IEnumerable<ContentItem> QueryContentItems(Guid contentTypeId, string[] searchOnColumns, string search, string[] filters, int pageSize, int pageNumber, string orderBy, IDbTransaction transaction = null)
    {
        //get the content type being queried
        var contentType = _entityFramework.ContentTypes
            .Include(p => p.ContentTypeFields)
            .ThenInclude(p => p.RelatedContentType)
            .ThenInclude(p => p.ContentTypeFields)
            .First(p => p.Id == contentTypeId);

        ContentType = contentType;

        //Assemble the SQL select and from statements
        var sqlBuilder = PrepareFrom(PrepareContentItemsDataSelect(new SqlQueryBuilder()));

        //Attach the where clause to filter by this content type
        sqlBuilder.AndWhere($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.ContentTypeId.Name} = '{contentTypeId}'");

        //Attach where clauses to that filter by a search query
        sqlBuilder = PrepareSearch(sqlBuilder, search, searchOnColumns);

        //Attach where clauses for odata filters
        sqlBuilder = PrepareODataFilters(sqlBuilder, filters);

        //Attach order by clauses
        sqlBuilder = PrepareOrderBy(sqlBuilder, orderBy);

        //Attach pagination
        pageNumber = pageNumber <= 1 ? 0 : pageNumber - 1;
        int skip = pageNumber * pageSize;

        var rawSql = $"{sqlBuilder.Build()} OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY";

        var resultFromQuery = (IEnumerable<IDictionary<string, object>>)_db.Query(rawSql, new { search = $"%{search}%", exactsearch = $"{search}" }, transaction: transaction);

        var items = new List<ContentItem>();
        if (resultFromQuery == null)
            return items;

        foreach (var rawResultItem in resultFromQuery)
        {
            var contentItem = MapQueryItemToContentItem(rawResultItem);
            items.Add(contentItem);
        }

        return items;
    }

    public IEnumerable<ContentItem> QueryAllContentItemsAsTransaction(Guid contentTypeId, string[] searchOnColumns, string search, string[] filters, string orderBy)
    {
        if (_db.State == ConnectionState.Closed)
        {
            _db.Open();
        }
        using (IDbTransaction transaction = _db.BeginTransaction(IsolationLevel.Snapshot))
        {
            int count = CountContentItems(contentTypeId, searchOnColumns, search, filters, transaction: transaction);

            int totalPages = (int)Math.Ceiling((double)count / View.DEFAULT_MAX_ITEMS_PER_PAGE);
            for (int pageIndex = 1; pageIndex <= totalPages; pageIndex++)
            {
                foreach (var item in QueryContentItems(contentTypeId, searchOnColumns, search, filters, View.DEFAULT_MAX_ITEMS_PER_PAGE, pageIndex, orderBy, transaction: transaction))
                {
                    yield return item;
                }
            }

            transaction.Commit();
        }
    }

    public int CountContentItems(Guid contentTypeId, string[] searchOnColumns, string search, string[] filters, IDbTransaction transaction = null)
    {
        //get the content type for what we are filtering by
        var contentType = _entityFramework.ContentTypes
            .Include(p => p.ContentTypeFields)
            .ThenInclude(p => p.RelatedContentType)
            .First(p => p.Id == contentTypeId);

        ContentType = contentType;

        //Assemble the sql select and from clauses
        var sqlBuilder = new SqlQueryBuilder();
        sqlBuilder = PrepareFrom(sqlBuilder.Select("COUNT(*) as Count"));

        //Attach the where clause to filter by this content type
        sqlBuilder.AndWhere($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.ContentTypeId.Name} = '{contentTypeId}'");

        //Attach the where clause if we are filtering by a search query
        sqlBuilder = PrepareSearch(sqlBuilder, search, searchOnColumns);

        //Attach where clauses for odata filters
        sqlBuilder = PrepareODataFilters(sqlBuilder, filters);

        var rawSql = sqlBuilder.Build();

        var numResults = _db.Query(rawSql, new { search = $"%{search}%", exactsearch = $"{search}" }, transaction: transaction).First().Count;
        return numResults;
    }

    private SqlQueryBuilder PrepareContentItemsDataSelect(SqlQueryBuilder sqlBuilder)
    {
        sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.ContentItemColumns(), RawSqlColumn.SOURCE_ITEM_COLUMN_NAME).ToArray());
        sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.RouteColumns(), RawSqlColumn.ROUTE_COLUMN_NAME).ToArray());
        sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.UserColumns(), RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME).ToArray());
        sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.UserColumns(), RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME).ToArray());
        foreach (var item in OneToOneRelationshipFields)
        {
            int index = OneToOneRelationshipFields.IndexOf(item);
            sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.ContentItemColumns(), $"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}").ToArray());
            sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.RouteColumns(), $"{RawSqlColumn.RELATED_ROUTE_COLUMN_NAME}_{index}").ToArray());
            sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.UserColumns(), $"{RawSqlColumn.RELATED_CREATED_BY_COLUMN_NAME}_{index}").ToArray());
            sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.UserColumns(), $"{RawSqlColumn.RELATED_MODIFIED_BY_COLUMN_NAME}_{index}").ToArray());
        }
        return sqlBuilder;
    }

    private SqlQueryBuilder PrepareFrom(SqlQueryBuilder sqlBuilder)
    {
        sqlBuilder.From($"{RawSqlColumn.CONTENT_ITEM_TABLE_NAME} AS {RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}");
        foreach (var item in OneToOneRelationshipFields)
        {
            int index = OneToOneRelationshipFields.IndexOf(item);
            sqlBuilder.Join($"{RawSqlColumn.CONTENT_ITEM_TABLE_NAME} AS {RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}", $"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}.{RawSqlColumn.Id.Name} = JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{item.DeveloperName?.ToDeveloperName()}')", joinType: "LEFT");
            sqlBuilder.Join($"{RawSqlColumn.USERS_TABLE_NAME} AS {RawSqlColumn.RELATED_CREATED_BY_COLUMN_NAME}_{index}", $"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}.{RawSqlColumn.CreatorUserId.Name} = {RawSqlColumn.RELATED_CREATED_BY_COLUMN_NAME}_{index}.{RawSqlColumn.Id.Name}", joinType: "LEFT");
            sqlBuilder.Join($"{RawSqlColumn.USERS_TABLE_NAME} AS {RawSqlColumn.RELATED_MODIFIED_BY_COLUMN_NAME}_{index}", $"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}.{RawSqlColumn.LastModifierUserId.Name} = {RawSqlColumn.RELATED_MODIFIED_BY_COLUMN_NAME}_{index}.{RawSqlColumn.Id.Name}", joinType: "LEFT");
            sqlBuilder.Join($"{RawSqlColumn.ROUTES_TABLE_NAME} AS {RawSqlColumn.RELATED_ROUTE_COLUMN_NAME}_{index}", $"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}.{RawSqlColumn.RouteId.Name} = {RawSqlColumn.RELATED_ROUTE_COLUMN_NAME}_{index}.{RawSqlColumn.Id.Name}", joinType: "LEFT");
        }
        sqlBuilder.Join($"{RawSqlColumn.ROUTES_TABLE_NAME} AS {RawSqlColumn.ROUTE_COLUMN_NAME} ", $"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.RouteId.Name} = {RawSqlColumn.ROUTE_COLUMN_NAME}.{RawSqlColumn.Id.Name}", joinType: "LEFT");
        sqlBuilder.Join($"{RawSqlColumn.USERS_TABLE_NAME} AS {RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}", $"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.CreatorUserId.Name} = {RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}.{RawSqlColumn.Id.Name}", joinType: "LEFT");
        sqlBuilder.Join($"{RawSqlColumn.USERS_TABLE_NAME} AS {RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}", $"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.LastModifierUserId.Name} = {RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}.{RawSqlColumn.Id.Name}", joinType: "LEFT");

        return sqlBuilder;
    }

    private SqlQueryBuilder PrepareODataFilters(SqlQueryBuilder sqlBuilder, string[] filters)
    {
        if (filters == null || !filters.Any())
            return sqlBuilder;

        var oDataToSql = new ODataFilterToSql(ContentType, PrimaryFieldDeveloperName, OneToOneRelationshipFields);
        foreach (var filter in filters.Where(p => !string.IsNullOrEmpty(p)))
        {
            string whereStatement = oDataToSql.GenerateSql(filter);
            if (!string.IsNullOrWhiteSpace(whereStatement))
                sqlBuilder.AndWhere($"({whereStatement})");
        }
        return sqlBuilder;
    }

    private SqlQueryBuilder PrepareSearch(SqlQueryBuilder sqlBuilder, string search, string[] searchOnColumns = null)
    {
        if (string.IsNullOrWhiteSpace(search))
            return sqlBuilder;

        var oDataToSql = new ODataFilterToSql(ContentType, PrimaryFieldDeveloperName, OneToOneRelationshipFields);

        //If no columns specified, just search on Primary Field only
        if (searchOnColumns == null || !searchOnColumns.Any())
            return sqlBuilder.AndWhere(oDataToSql.GenerateSql($"contains({BuiltInContentTypeField.PrimaryField.DeveloperName}, '{search}')"));

        string originalSearch = search;
        search = search.ToLower();

        var searchFilters = new List<string>();

        //For each column specified, generate the appropriate OData filter or Raw Sql search clause
        foreach (var column in searchOnColumns)
        {
            var columnAsContentTypeField = ContentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == column);
            if (columnAsContentTypeField != null)
            {
                string columnAsContentTypeFieldDeveloperName = columnAsContentTypeField.DeveloperName.ToDeveloperName();

                if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Number && decimal.TryParse(search, out var searchAsDecimal))
                {
                    searchFilters.Add(oDataToSql.GenerateSql($"{columnAsContentTypeFieldDeveloperName} eq '{search}'"));
                }
                else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Checkbox && bool.TryParse(search, out var searchAsBool))
                {
                    searchFilters.Add(oDataToSql.GenerateSql($"{columnAsContentTypeFieldDeveloperName} eq '{search}'"));
                }
                else
                {
                    searchFilters.Add(oDataToSql.GenerateSql($"contains({columnAsContentTypeFieldDeveloperName}, '{search}')"));
                }
            }
            else
            {
                var reservedField = BuiltInContentTypeField.ReservedContentTypeFields.FirstOrDefault(p => p.DeveloperName == column);
                if (reservedField != null)
                {
                    if (reservedField.DeveloperName == BuiltInContentTypeField.Id && ShortGuid.TryParse(originalSearch.Trim(), out ShortGuid searchAsShortGuid))
                    {
                        searchFilters.Add(oDataToSql.GenerateSql($"{reservedField.DeveloperName} eq 'guid_{originalSearch.Trim()}'"));
                    }
                    else if (reservedField.DeveloperName == BuiltInContentTypeField.CreatorUser.DeveloperName)
                    {
                        searchFilters.Add($"{RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}.{RawSqlColumn.FirstName.Name} COLLATE Latin1_General_CI_AS LIKE @search");
                        searchFilters.Add($"{RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}.{RawSqlColumn.LastName.Name} COLLATE Latin1_General_CI_AS LIKE @search");
                    }
                    else if (reservedField.DeveloperName == BuiltInContentTypeField.LastModifierUser.DeveloperName)
                    {
                        searchFilters.Add($"{RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}.{RawSqlColumn.FirstName.Name} COLLATE Latin1_General_CI_AS LIKE @search");
                        searchFilters.Add($"{RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}.{RawSqlColumn.LastName.Name} COLLATE Latin1_General_CI_AS LIKE @search");
                    }
                    else if (reservedField.DeveloperName == BuiltInContentTypeField.PrimaryField.DeveloperName)
                    {
                        searchFilters.Add(oDataToSql.GenerateSql($"contains({reservedField.DeveloperName}, '{search}')"));
                    }
                }
            }
        }

        //Combine everything by OR operator
        string oDataSearchWhereFilterClause = string.Join(" OR ", searchFilters.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (!string.IsNullOrEmpty(oDataSearchWhereFilterClause))
        {
            sqlBuilder.AndWhere($"({oDataSearchWhereFilterClause})");
        }

        return sqlBuilder;
    }

    private SqlQueryBuilder PrepareOrderBy(SqlQueryBuilder sqlBuilder, string orderBy)
    {
        if (string.IsNullOrEmpty(orderBy))
            return sqlBuilder;

        var orderBySplit = orderBy.Split(",");
        List<string> orderByClauses = new List<string>();

        //orderBy comes in form like "PrimaryField asc, CreationTime desc"
        //Need to assemble SQL on each one separately
        foreach (var orderByElement in orderBySplit)
        {
            try
            {
                //orderByElement comes in form like 'PrimaryField asc'
                //Need to split this up into column and direction
                (var column, var direction) = orderByElement.SplitIntoColumnAndSortOrder();

                var columnAsContentTypeField = ContentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == column);
                if (columnAsContentTypeField != null)
                {
                    string columnAsContentTypeFieldDeveloperName = columnAsContentTypeField.DeveloperName;
                    if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
                    {
                        var relatedObjectField = OneToOneRelationshipFields.First(p => p.DeveloperName == columnAsContentTypeField.DeveloperName);
                        int indexOfRelatedObject = OneToOneRelationshipFields.ToList().IndexOf(relatedObjectField);
                        string relatedObjPrimaryFieldName = relatedObjectField.ContentType.ContentTypeFields.First(p => p.Id == relatedObjectField.ContentType.PrimaryFieldId).DeveloperName.ToDeveloperName();
                        sqlBuilder.OrderBy(columnAsContentTypeField.FieldType.SqlServerOrderByExpression($"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{indexOfRelatedObject}", RawSqlColumn.PublishedContent.Name, relatedObjPrimaryFieldName, direction.DeveloperName));
                    }
                    else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Date)
                    {
                        int sqlDateOutput = _currentOrganization.DateFormat switch
                        {
                            DateTimeExtensions.MM_dd_yyyy => 101,
                            DateTimeExtensions.dd_MM_yyyy => 103,
                            _ => 0
                        };

                        sqlBuilder.OrderBy(columnAsContentTypeField.FieldType.SqlServerOrderByExpression(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, columnAsContentTypeField.DeveloperName, sqlDateOutput.ToString(), direction.DeveloperName));
                    }
                    else 
                    {
                        sqlBuilder.OrderBy(columnAsContentTypeField.FieldType.SqlServerOrderByExpression(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, columnAsContentTypeField.DeveloperName, direction.DeveloperName));
                    }
                }
                else
                {
                    var reservedField = BuiltInContentTypeField.ReservedContentTypeFields.FirstOrDefault(p => p.DeveloperName == column);
                    if (reservedField != null)
                    {
                        if (reservedField.DeveloperName == BuiltInContentTypeField.PrimaryField)
                        {
                            sqlBuilder.OrderBy(reservedField.FieldType.SqlServerOrderByExpression(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, PrimaryFieldDeveloperName, direction.DeveloperName));
                        }
                        else if (reservedField.DeveloperName == BuiltInContentTypeField.CreatorUser)
                        {
                            sqlBuilder.OrderBy($"{RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}.{RawSqlColumn.FirstName.Name} {direction.DeveloperName}");
                        }
                        else if (reservedField.DeveloperName == BuiltInContentTypeField.LastModifierUser)
                        {
                            sqlBuilder.OrderBy($"{RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}.{RawSqlColumn.FirstName.Name} {direction.DeveloperName}");
                        }
                        else
                        {
                            sqlBuilder.OrderBy($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{reservedField.DeveloperName} {direction.DeveloperName}");
                        }
                    }
                }
            }
            catch
            {
            }
        }

        return sqlBuilder;
    }

    private ContentItem MapQueryItemToContentItem(IDictionary<string, object> resultFromQuery)
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

    private T ToStatic<T>(IEnumerable<KeyValuePair<string, object>> properties)
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

    private dynamic ConvertJsonContentToDynamic(string contentAsJson, ContentType contentType, IEnumerable<ContentItem> relatedContentItems = null)
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

