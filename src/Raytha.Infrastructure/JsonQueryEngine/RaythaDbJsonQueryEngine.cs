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

    public RaythaDbJsonQueryEngine(IRaythaDbContext entityFramework, IDbConnection db, ICurrentOrganization currentOrganization)
    {
        _entityFramework = entityFramework;
        _db = db;
        _currentOrganization = currentOrganization;
    }

    public ContentItem FirstOrDefault(Guid entityId)
    {
        var entity = _entityFramework.ContentItems
            .Include(p => p.CreatorUser)
            .Include(p => p.LastModifierUser)
            .Include(p => p.Route)
            .Include(p => p.ContentType)
            .ThenInclude(p => p.ContentTypeFields)
            .ThenInclude(p => p.RelatedContentType)
            .ThenInclude(p => p.ContentTypeFields)
            .FirstOrDefault(p => p.Id == entityId);

        List<ContentTypeField> oneToOneRelationshipFields = entity.ContentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship && p.RelatedContentTypeId.HasValue).ToList();

        var sqlBuilder = PrepareFrom(PrepareContentItemsDataSelect(new SqlQueryBuilder(), oneToOneRelationshipFields), oneToOneRelationshipFields);
        sqlBuilder.AndWhere($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.Id.Name} = '{entityId}'");

        var sqlStatement = sqlBuilder.Build();

        IDictionary<string, object> resultFromQuery = _db.QuerySingle(sqlStatement);
        if (resultFromQuery == null)
            return null;


        List<ContentItem> relatedContentItems = new List<ContentItem>();
        foreach (var item in oneToOneRelationshipFields)
        {
            int index = oneToOneRelationshipFields.IndexOf(item);
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
        contentItem.PublishedContent = ConvertJsonContentToDynamic(contentItem._PublishedContent, entity.ContentType, relatedContentItems);
        contentItem.DraftContent = ConvertJsonContentToDynamic(contentItem._DraftContent, entity.ContentType, relatedContentItems);
        
        string primaryFieldDeveloperName = entity.ContentType.ContentTypeFields.First(p => p.Id == entity.ContentType.PrimaryFieldId).DeveloperName;
        if (contentItem.PublishedContent.ContainsKey(primaryFieldDeveloperName))
            contentItem.PrimaryField = contentItem.PublishedContent[primaryFieldDeveloperName];

        contentItem.CreatorUser = entity.CreatorUser;
        contentItem.LastModifierUser = entity.LastModifierUser;
        contentItem.ContentType = entity.ContentType;
        contentItem.ContentTypeId = entity.ContentTypeId;
        contentItem.Route = entity.Route;
        return contentItem;
    }


    public IEnumerable<ContentItem> QueryContentItems(Guid contentTypeId, string[] searchOnColumns, string search, string[] filters, int pageSize, int pageNumber, string orderBy, IDbTransaction transaction = null)
    {
        var contentType = _entityFramework.ContentTypes
            .Include(p => p.ContentTypeFields)
            .ThenInclude(p => p.RelatedContentType)
            .ThenInclude(p => p.ContentTypeFields)
            .First(p => p.Id == contentTypeId);

        List<ContentTypeField> oneToOneRelationshipFields = contentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship && p.RelatedContentTypeId.HasValue).ToList();
        var primaryFieldName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName;

        var sqlBuilder = PrepareFrom(PrepareContentItemsDataSelect(new SqlQueryBuilder(), oneToOneRelationshipFields), oneToOneRelationshipFields);
        sqlBuilder.AndWhere($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.ContentTypeId.Name} = '{contentTypeId}'");

        if (searchOnColumns == null || !searchOnColumns.Any())
        {
            string whereStatement = CreateSqlSearchWhereStatement(contentType, search);
            sqlBuilder.AndWhere(whereStatement);
        }
        else
        {
            string whereStatement = CreateSqlSearchWhereStatement(contentType, search, searchOnColumns);
            sqlBuilder.AndWhere(whereStatement);
        }

        var oDataToSql = new ODataFilterToSql(contentType, primaryFieldName, oneToOneRelationshipFields);
        foreach (var filter in filters.Where(p => !string.IsNullOrEmpty(p)))
        {
            string whereStatement = oDataToSql.GenerateSql(filter);
            if (!string.IsNullOrWhiteSpace(whereStatement))
                sqlBuilder.AndWhere($"({whereStatement})");
        }


        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            string orderByClause = CreateSqlOrderByStatement(contentType, orderBy);
            sqlBuilder.OrderBy(orderByClause);
        }

        pageNumber = pageNumber <= 1 ? 0 : pageNumber - 1;
        int skip = pageNumber * pageSize;

        var rawSql = $"{sqlBuilder.Build()} OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY";

        var resultFromQuery = (IEnumerable<IDictionary<string, object>>)_db.Query(rawSql, new { search = $"%{search}%", exactsearch = $"{search}" }, transaction: transaction);

        var items = new List<ContentItem>();
        if (resultFromQuery == null)
            return items;

        foreach (var rawResultItem in resultFromQuery)
        {
            List<ContentItem> relatedContentItems = new List<ContentItem>();
            foreach (var item in oneToOneRelationshipFields)
            {
                int index = oneToOneRelationshipFields.IndexOf(item);
                var relatedCreatorUser = ToStatic<User>(rawResultItem.FilterAndTrimKeys($"{RawSqlColumn.RELATED_CREATED_BY_COLUMN_NAME}_{index}_"));
                var relatedModifierUser = ToStatic<User>(rawResultItem.FilterAndTrimKeys($"{RawSqlColumn.RELATED_MODIFIED_BY_COLUMN_NAME}_{index}_"));
                var relatedRoute = ToStatic<Route>(rawResultItem.FilterAndTrimKeys($"{RawSqlColumn.RELATED_ROUTE_COLUMN_NAME}_{index}_"));
                var relatedItem = ToStatic<ContentItem>(rawResultItem.FilterAndTrimKeys($"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}_"));
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

            var creatorUser = ToStatic<User>(rawResultItem.FilterAndTrimKeys($"{RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}_"));
            var modifierUser = ToStatic<User>(rawResultItem.FilterAndTrimKeys($"{RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}_"));
            var route = ToStatic<Route>(rawResultItem.FilterAndTrimKeys($"{RawSqlColumn.ROUTE_COLUMN_NAME}_"));
            var contentItem = ToStatic<ContentItem>(rawResultItem.FilterAndTrimKeys($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}_"));
            contentItem.PublishedContent = ConvertJsonContentToDynamic(contentItem._PublishedContent, contentType, relatedContentItems);
            contentItem.DraftContent = ConvertJsonContentToDynamic(contentItem._DraftContent, contentType, relatedContentItems);

            string primaryFieldDeveloperName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName;
            if (contentItem.PublishedContent.ContainsKey(primaryFieldDeveloperName))
                contentItem.PrimaryField = contentItem.PublishedContent[primaryFieldDeveloperName];

            contentItem.CreatorUser = creatorUser;
            contentItem.LastModifierUser = modifierUser;
            contentItem.ContentTypeId = contentTypeId;
            contentItem.ContentType = contentType;
            contentItem.Route = route;
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
        var contentType = _entityFramework.ContentTypes
            .Include(p => p.ContentTypeFields)
            .ThenInclude(p => p.RelatedContentType)
            .First(p => p.Id == contentTypeId);

        List<ContentTypeField> oneToOneRelationshipFields = contentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship).ToList();
        var primaryFieldName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName;

        var sqlBuilder = new SqlQueryBuilder();
        sqlBuilder = PrepareFrom(sqlBuilder.Select("COUNT(*) as Count"), oneToOneRelationshipFields);

        sqlBuilder.AndWhere($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.ContentTypeId.Name} = '{contentTypeId}'");

        if (searchOnColumns == null || !searchOnColumns.Any())
        {
            string whereStatement = CreateSqlSearchWhereStatement(contentType, search);
            sqlBuilder.AndWhere(whereStatement);
        }
        else
        {
            string whereStatement = CreateSqlSearchWhereStatement(contentType, search, searchOnColumns);
            sqlBuilder.AndWhere(whereStatement);
        }

        var oDataToSql = new ODataFilterToSql(contentType, primaryFieldName, oneToOneRelationshipFields);
        foreach (var filter in filters.Where(p => !string.IsNullOrEmpty(p)))
        {
            string whereStatement = oDataToSql.GenerateSql(filter);
            if (!string.IsNullOrWhiteSpace(whereStatement))
                sqlBuilder.AndWhere($"({whereStatement})");
        }

        var rawSql = sqlBuilder.Build();

        var numResults = _db.Query(rawSql, new { search = $"%{search}%", exactsearch = $"{search}" }, transaction: transaction).First().Count;
        return numResults;
    }

    private SqlQueryBuilder PrepareContentItemsDataSelect(SqlQueryBuilder sqlBuilder, List<ContentTypeField> oneToOneRelationshipFields)
    {
        sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.ContentItemColumns(), RawSqlColumn.SOURCE_ITEM_COLUMN_NAME).ToArray());
        sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.RouteColumns(), RawSqlColumn.ROUTE_COLUMN_NAME).ToArray());
        sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.UserColumns(), RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME).ToArray());
        sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.UserColumns(), RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME).ToArray());
        foreach (var item in oneToOneRelationshipFields)
        {
            int index = oneToOneRelationshipFields.IndexOf(item);
            sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.ContentItemColumns(), $"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}").ToArray());
            sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.RouteColumns(), $"{RawSqlColumn.RELATED_ROUTE_COLUMN_NAME}_{index}").ToArray());
            sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.UserColumns(), $"{RawSqlColumn.RELATED_CREATED_BY_COLUMN_NAME}_{index}").ToArray());
            sqlBuilder.Select(RawSqlColumn.NameAsFullColumnLabelForEnumerable(RawSqlColumn.UserColumns(), $"{RawSqlColumn.RELATED_MODIFIED_BY_COLUMN_NAME}_{index}").ToArray());
        }
        return sqlBuilder;
    }

    private SqlQueryBuilder PrepareFrom(SqlQueryBuilder sqlBuilder, List<ContentTypeField> oneToOneRelationshipFields)
    {
        sqlBuilder.From($"{RawSqlColumn.CONTENT_ITEM_TABLE_NAME} AS {RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}");
        foreach (var item in oneToOneRelationshipFields)
        {
            int index = oneToOneRelationshipFields.IndexOf(item);
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

    private string CreateSqlSearchWhereStatement(ContentType contentType, string search, string[] searchOnColumns)
    {
        if (string.IsNullOrEmpty(search))
            return string.Empty;
        string originalSearch = search;
        search = search.ToLower();

        List<ContentTypeField> oneToOneRelationshipFields = contentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship).ToList();
        var primaryFieldName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName.ToDeveloperName();

        var searchClauses = new List<string>();
        foreach (var column in searchOnColumns)
        {
            var columnAsContentTypeField = contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == column);
            if (columnAsContentTypeField != null)
            {
                string columnAsContentTypeFieldDeveloperName = columnAsContentTypeField.DeveloperName.ToDeveloperName();
                if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Number)
                {
                    decimal numericValue;
                    if (decimal.TryParse(search, out numericValue))
                    {
                        searchClauses.Add($"JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}') = '{numericValue}'");
                    }
                }
                else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
                {
                    var relatedObjectField = oneToOneRelationshipFields.First(p => p.DeveloperName == columnAsContentTypeField.DeveloperName);
                    int indexOfRelatedObject = oneToOneRelationshipFields.ToList().IndexOf(relatedObjectField);
                    string relatedObjPrimaryFieldName = relatedObjectField.ContentType.ContentTypeFields.First(p => p.Id == relatedObjectField.ContentType.PrimaryFieldId).DeveloperName.ToDeveloperName();
                    searchClauses.Add($"JSON_VALUE({RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{indexOfRelatedObject}.{RawSqlColumn.PublishedContent.Name}, '$.{relatedObjPrimaryFieldName}') COLLATE Latin1_General_CI_AS LIKE @search");
                }
                else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.MultipleSelect)
                {
                    if (columnAsContentTypeField.Choices.Any(p => p.DeveloperName == search))
                    {
                        searchClauses.Add($"(ISJSON(JSON_QUERY({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}'))) = 1 AND EXISTS (SELECT * FROM OPENJSON({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}') as temp WHERE temp.value = @exactsearch)");
                    }
                }
                else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Checkbox)
                {
                    if (search == "true" || search == "false")
                    {
                        searchClauses.Add($"JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}') = @exactsearch");
                    }
                }
                else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Date)
                {
                    int sqlDateOutput = 0;
                    if (_currentOrganization.DateFormat == DateTimeExtensions.MM_dd_yyyy)
                    {
                        sqlDateOutput = 101;
                    }
                    else if (_currentOrganization.DateFormat == DateTimeExtensions.dd_MM_yyyy)
                    {
                        sqlDateOutput = 103;
                    }
                    searchClauses.Add($"TRY_CONVERT(varchar, TRY_CONVERT(datetime, JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}')), {sqlDateOutput}) COLLATE Latin1_General_CI_AS LIKE @search");
                }
                else
                {
                    searchClauses.Add($"JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}') COLLATE Latin1_General_CI_AS LIKE @search");
                }
            }
            else
            {
                var reservedField = BuiltInContentTypeField.ReservedContentTypeFields.FirstOrDefault(p => p.DeveloperName == column);
                if (reservedField != null)
                {
                    if (reservedField.DeveloperName == BuiltInContentTypeField.Id)
                    {
                        ShortGuid shortGuid;
                        if (ShortGuid.TryParse(originalSearch, out shortGuid))
                        {
                            searchClauses.Add($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.Id.Name} = '{shortGuid.Guid}'");
                        }
                    }
                    else if (reservedField.DeveloperName == BuiltInContentTypeField.CreatorUser.DeveloperName)
                    {
                        searchClauses.Add($"{RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}.FirstName COLLATE Latin1_General_CI_AS LIKE @search");
                        searchClauses.Add($"{RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}.LastName COLLATE Latin1_General_CI_AS LIKE @search");
                    }
                    else if (reservedField.DeveloperName == BuiltInContentTypeField.LastModifierUser.DeveloperName)
                    {
                        searchClauses.Add($"{RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}.FirstName COLLATE Latin1_General_CI_AS LIKE @search");
                        searchClauses.Add($"{RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}.LastName COLLATE Latin1_General_CI_AS LIKE @search");
                    }
                    else if (reservedField.DeveloperName == BuiltInContentTypeField.PrimaryField.DeveloperName)
                    {
                        searchClauses.Add($"JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{primaryFieldName}') COLLATE Latin1_General_CI_AS LIKE @search");
                    }
                }
            }
        }
        if (searchClauses.Any())
        {
            return string.Join(" OR ", searchClauses.Distinct());
        }
        else
        {
            return string.Empty;
        }
    }

    private string CreateSqlSearchWhereStatement(ContentType contentType, string search)
    {
        if (string.IsNullOrEmpty(search))
            return string.Empty;

        var primaryFieldName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName.ToDeveloperName();
        return $"JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{primaryFieldName}') COLLATE Latin1_General_CI_AS LIKE @search";
    }

    private string CreateSqlOrderByStatement(ContentType contentType, string orderBy)
    {
        var orderBySplit = orderBy.Split(",");
        List<string> orderByClauses = new List<string>();

        List<ContentTypeField> oneToOneRelationshipFields = contentType.ContentTypeFields.Where(p => p.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship).ToList();
        var primaryFieldName = contentType.ContentTypeFields.First(p => p.Id == contentType.PrimaryFieldId).DeveloperName.ToDeveloperName();

        foreach (var orderByElement in orderBySplit)
        {
            try
            {
                var column = orderByElement.Split(" ")[0];
                var direction = Raytha.Domain.ValueObjects.SortOrder.From(orderByElement.Split(" ")[1]);

                var columnAsContentTypeField = contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == column);
                if (columnAsContentTypeField != null)
                {
                    string columnAsContentTypeFieldDeveloperName = columnAsContentTypeField.DeveloperName;
                    if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Number)
                    {
                        orderByClauses.Add($"CAST(JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}') AS decimal) {direction.DeveloperName}");                      
                    }
                    else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
                    {
                        var relatedObjectField = oneToOneRelationshipFields.First(p => p.DeveloperName == columnAsContentTypeField.DeveloperName);
                        int indexOfRelatedObject = oneToOneRelationshipFields.ToList().IndexOf(relatedObjectField);
                        string relatedObjPrimaryFieldName = relatedObjectField.ContentType.ContentTypeFields.First(p => p.Id == relatedObjectField.ContentType.PrimaryFieldId).DeveloperName.ToDeveloperName();
                        orderByClauses.Add($"JSON_VALUE({RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{indexOfRelatedObject}.{RawSqlColumn.PublishedContent.Name}, '$.{relatedObjPrimaryFieldName}') {direction.DeveloperName}");
                    }
                    else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Checkbox)
                    {
                        orderByClauses.Add($"JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}') {direction.DeveloperName}");                       
                    }
                    else if (columnAsContentTypeField.FieldType.DeveloperName == BaseFieldType.Date)
                    {
                        int sqlDateOutput = 0;
                        if (_currentOrganization.DateFormat == DateTimeExtensions.MM_dd_yyyy)
                        {
                            sqlDateOutput = 101;
                        }
                        else if (_currentOrganization.DateFormat == DateTimeExtensions.dd_MM_yyyy)
                        {
                            sqlDateOutput = 103;
                        }
                        orderByClauses.Add($"TRY_CONVERT(datetime, JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}'), {sqlDateOutput}) {direction.DeveloperName}");
                    }
                    else if (columnAsContentTypeField.FieldType.DeveloperName != BaseFieldType.MultipleSelect)
                    {
                        orderByClauses.Add($"JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{columnAsContentTypeFieldDeveloperName}') {direction.DeveloperName}");
                    }
                }
                else
                {
                    var reservedField = BuiltInContentTypeField.ReservedContentTypeFields.FirstOrDefault(p => p.DeveloperName == column);
                    if (reservedField != null)
                    {
                        if (reservedField.DeveloperName == BuiltInContentTypeField.PrimaryField)
                        {
                            orderByClauses.Add($"JSON_VALUE({RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{RawSqlColumn.PublishedContent.Name}, '$.{primaryFieldName}') {direction.DeveloperName}");
                        }
                        else if (reservedField.DeveloperName == BuiltInContentTypeField.CreatorUser)
                        {
                            orderByClauses.Add($"{RawSqlColumn.SOURCE_CREATED_BY_COLUMN_NAME}.FirstName {direction.DeveloperName}");
                        }
                        else if (reservedField.DeveloperName == BuiltInContentTypeField.LastModifierUser)
                        {
                            orderByClauses.Add($"{RawSqlColumn.SOURCE_MODIFIED_BY_COLUMN_NAME}.FirstName {direction.DeveloperName}");
                        }
                        else
                        {
                            orderByClauses.Add($"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{reservedField.DeveloperName} {direction.DeveloperName}");
                        }
                    }
                }
            }
            catch
            {
            }
        }

        return string.Join(",", orderByClauses);
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

