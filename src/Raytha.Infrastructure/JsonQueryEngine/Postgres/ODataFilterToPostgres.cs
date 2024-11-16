using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Infrastructure.JsonQueryEngine.Postgres;

internal class ODataFilterToPostgres : AbstractODataFilterToSql
{
    public ODataFilterToPostgres(ContentType contentType, string primaryFieldName, IEnumerable<ContentTypeField> relatedObjectFields, string dateTimeFormat)
    {
        _contentType = contentType;
        _primaryFieldName = primaryFieldName;
        _relatedObjectFields = relatedObjectFields;
        _dateTimeFormat = dateTimeFormat;
    }

    public override string GenerateSql(string filterExpression)
    {
        if (string.IsNullOrEmpty(filterExpression))
            return string.Empty;

        Dictionary<string, string> odataOptionsAsDictionary = new Dictionary<string, string>
            {
                {"$filter", filterExpression }
            };

        IEdmModel edmModel = GetEdmModel();
        IEdmNavigationSource edmNavigationSource = edmModel.FindDeclaredEntitySet(placeholderClassName);
        IEdmType edmType = edmNavigationSource.Type;

        ODataQueryOptionParser parser = new ODataQueryOptionParser(edmModel, edmType, edmNavigationSource, odataOptionsAsDictionary);

        FilterClause parsedODataFilterClause = parser.ParseFilter();

        ODataFilterToSqlVisitor<object> visitor = new ODataFilterToSqlVisitor<object>(_contentType, _primaryFieldName, _relatedObjectFields, _dateTimeFormat);
        parsedODataFilterClause.Expression.Accept(visitor);

        string whereClause = visitor.GetWhereClause();
        return whereClause;
    }

    private class ODataFilterToSqlVisitor<TSource> : AbstractODataFilterToSqlVisitor<TSource> where TSource : class
    {
        public ODataFilterToSqlVisitor(ContentType contentType, string primaryFieldName, IEnumerable<ContentTypeField> relatedObjectFields, string dateTimeFormat)
        {
            _contentType = contentType;
            _primaryFieldName = primaryFieldName;
            _relatedObjectFields = relatedObjectFields;
            _dateTimeFormat = dateTimeFormat;
        }

        public override TSource Visit(SingleValueFunctionCallNode nodeIn)
        {
            string functionName = nodeIn.Name;
            SingleValueOpenPropertyAccessNode fieldName = ((ConvertNode)nodeIn.Parameters.ToArray()[0]).Source as SingleValueOpenPropertyAccessNode;
            ConstantNode value = nodeIn.Parameters.ToArray()[1] as ConstantNode;

            var realFieldName = UseDeveloperNameForPrimaryField(fieldName.Name);

            if (BuiltInContentTypeField.ReservedContentTypeFields.Any(p => p.DeveloperName == realFieldName))
            {
                string prefix = $"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.\"{realFieldName}\" ILIKE";
                whereClause.Append($"{prefix} {value.Value.ToString().ApplySqlStringLikeOperator}");
            }
            else
            {
                var chosenColumnAsCustomField = _contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == realFieldName);
                if (chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
                {
                    var relatedObjectField = _relatedObjectFields.First(p => p.DeveloperName == chosenColumnAsCustomField.DeveloperName);
                    int indexOfRelatedObject = _relatedObjectFields.ToList().IndexOf(relatedObjectField);
                    string relatedObjPrimaryFieldName = relatedObjectField.ContentType.ContentTypeFields.First(p => p.Id == relatedObjectField.ContentType.PrimaryFieldId).DeveloperName;
                    whereClause.Append(chosenColumnAsCustomField.FieldType.PostgresLikeJsonValue($"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{indexOfRelatedObject}", RawSqlColumn.PublishedContent.Name, relatedObjPrimaryFieldName, value.Value.ToString().ApplySqlStringLikeOperator(functionName)));
                }
                else if (chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.MultipleSelect)
                {
                    if (functionName != "contains")
                        throw new NotImplementedException($"{functionName} is not an implemented function call.");

                    whereClause.Append(chosenColumnAsCustomField.FieldType.PostgresLikeJsonValue(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, realFieldName, value.Value.ToString()));
                }
                else
                {
                    whereClause.Append(chosenColumnAsCustomField.FieldType.PostgresLikeJsonValue(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, realFieldName, value.Value.ToString().ApplySqlStringLikeOperator(functionName)));
                }
            }

            return null;
        }

        protected override void HandleSingleValueProperty(string realFieldName)
        {
            if (BuiltInContentTypeField.ReservedContentTypeFields.Any(p => p.DeveloperName == realFieldName))
            {
                whereClause.Append($" {RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.\"{realFieldName}\" ");
            }
            else
            {
                var chosenColumnAsCustomField = _contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == realFieldName);
                if (chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
                {
                    var relatedObjectField = _relatedObjectFields.First(p => p.DeveloperName == chosenColumnAsCustomField.DeveloperName);
                    int indexOfRelatedObject = _relatedObjectFields.ToList().IndexOf(relatedObjectField);
                    string relatedObjPrimaryFieldName = relatedObjectField.ContentType.ContentTypeFields.First(p => p.Id == relatedObjectField.ContentType.PrimaryFieldId).DeveloperName;
                    whereClause.Append(chosenColumnAsCustomField.FieldType.PostgresSingleJsonValue($"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{indexOfRelatedObject}", RawSqlColumn.PublishedContent.Name, relatedObjPrimaryFieldName));
                }
                else if (chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.Date)
                {
                    whereClause.Append(chosenColumnAsCustomField.FieldType.PostgresSingleJsonValue(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, realFieldName, _dateTimeFormat));
                }
                else
                {
                    whereClause.Append(chosenColumnAsCustomField.FieldType.PostgresSingleJsonValue(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, realFieldName));
                }
            }
        }
    }
}
