using CSharpVitamins;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace Raytha.Infrastructure.JsonQueryEngine;

internal class ODataFilterToSql
{
    private ContentType _contentType;
    private string _primaryFieldName;
    private IEnumerable<ContentTypeField> _relatedObjectFields;
    public ODataFilterToSql(ContentType contentType, string primaryFieldName, IEnumerable<ContentTypeField> relatedObjectFields)
    {
        _contentType = contentType;
        _primaryFieldName = primaryFieldName;
        _relatedObjectFields = relatedObjectFields;
    }
    string placeholderClassName = typeof(PlaceholderClass).Name;
    public string GenerateSql(string filterExpression)
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

        ODataFilterToSqlVisitor<object> visitor = new ODataFilterToSqlVisitor<object>(_contentType, _primaryFieldName, _relatedObjectFields);
        parsedODataFilterClause.Expression.Accept(visitor);

        string whereClause = visitor.GetWhereClause();
        return whereClause;
    }

    private IEdmModel GetEdmModel()
    {
        ODataModelBuilder builder = new ODataConventionModelBuilder();
        builder.EntitySet<PlaceholderClass>(placeholderClassName);
        return builder.GetEdmModel();
    }

    private class PlaceholderClass
    {
        [Key]
        public int Id { get; set; }
        public IDictionary<string, object> Properties { get; set; }
    }

    private class ODataFilterToSqlVisitor<TSource> : QueryNodeVisitor<TSource> where TSource : class
    {
        private StringBuilder whereClause = new StringBuilder();
        private ContentType _contentType;
        private string _primaryFieldName;
        private IEnumerable<ContentTypeField> _relatedObjectFields;
        public ODataFilterToSqlVisitor(ContentType contentType, string primaryFieldName, IEnumerable<ContentTypeField> relatedObjectFields)
        {
            _contentType = contentType;
            _primaryFieldName = primaryFieldName;
            _relatedObjectFields = relatedObjectFields;
        }

        public string GetWhereClause()
        {
            return whereClause.ToString()
                .Replace("!= null", "IS NOT NULL")
                .Replace("= null", "IS NULL");
        }

        public override TSource Visit(SingleValuePropertyAccessNode nodeIn)
        {
            var realFieldName = this.UseDeveloperNameForPrimaryField(nodeIn.Property.Name);
            HandleSingleValueProperty(realFieldName);
            return null;
        }

        public override TSource Visit(ConstantNode nodeIn)
        {
            string literalText = this.TryParseGuidOrValue(nodeIn.LiteralText);
            whereClause.Append(literalText);
            return null;
        }

        public override TSource Visit(ConvertNode nodeIn)
        {
            HandleVisitNextNode(nodeIn.Source);
            return null;
        }

        public override TSource Visit(SingleValueFunctionCallNode nodeIn)
        {
            string functionName = nodeIn.Name;
            SingleValueOpenPropertyAccessNode fieldName = ((ConvertNode)nodeIn.Parameters.ToArray()[0]).Source as SingleValueOpenPropertyAccessNode;
            ConstantNode value = nodeIn.Parameters.ToArray()[1] as ConstantNode;

            var realFieldName = this.UseDeveloperNameForPrimaryField(fieldName.Name);

            if (BuiltInContentTypeField.ReservedContentTypeFields.Any(p => p.DeveloperName == realFieldName))
            {
                string prefix = $"{RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{realFieldName} COLLATE Latin1_General_CI_AS LIKE";
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
                    whereClause.Append(chosenColumnAsCustomField.FieldType.SqlServerLikeJsonValue($"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{indexOfRelatedObject}", RawSqlColumn.PublishedContent.Name, relatedObjPrimaryFieldName, value.Value.ToString().ApplySqlStringLikeOperator(functionName)));
                }
                else if (chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.MultipleSelect)
                {
                    if (functionName != "contains")
                        throw new NotImplementedException($"{functionName} is not an implemented function call.");

                    whereClause.Append(chosenColumnAsCustomField.FieldType.SqlServerLikeJsonValue(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, realFieldName, value.Value.ToString()));
                }
                else
                {
                    whereClause.Append(chosenColumnAsCustomField.FieldType.SqlServerLikeJsonValue(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, realFieldName, value.Value.ToString().ApplySqlStringLikeOperator(functionName)));
                }
            }

            return null;
        }

        public override TSource Visit(UnaryOperatorNode nodeIn)
        {
            if (nodeIn.OperatorKind == UnaryOperatorKind.Not)
                whereClause.Append(" NOT ");

            var nextNode = nodeIn.Operand;
            HandleVisitNextNode(nextNode);

            return null;
        }

        public override TSource Visit(BinaryOperatorNode nodeIn)
        {

            whereClause.Append("(");
            var bon = (BinaryOperatorNode)nodeIn;
            var left = bon.Left;

            HandleVisitNextNode(left);

            switch (nodeIn.OperatorKind)
            {
                case BinaryOperatorKind.And:
                    whereClause.Append(" AND ");
                    break;
                case BinaryOperatorKind.Or:
                    whereClause.Append(" OR ");
                    break;
                case BinaryOperatorKind.Equal:
                    whereClause.Append(" = ");
                    break;
                case BinaryOperatorKind.NotEqual:
                    whereClause.Append(" != ");
                    break;
                case BinaryOperatorKind.GreaterThan:
                    whereClause.Append(" > ");
                    break;
                case BinaryOperatorKind.GreaterThanOrEqual:
                    whereClause.Append(" >= ");
                    break;
                case BinaryOperatorKind.LessThan:
                    whereClause.Append(" < ");
                    break;
                case BinaryOperatorKind.LessThanOrEqual:
                    whereClause.Append(" <= ");
                    break;
                default:
                    throw new NotImplementedException($"Binary operator {nodeIn.OperatorKind} not supported.");
            }

            var right = bon.Right;
            HandleVisitNextNode(right);

            whereClause.Append(")");
            return null;
        }

        private string UseDeveloperNameForPrimaryField(string name)
        {
            if (name == BuiltInContentTypeField.PrimaryField)
            {
                return this._primaryFieldName;
            }
            return name;
        }

        private string TryParseGuidOrValue(string literalText)
        {
            var reg = new Regex("'guid_.*?'");
            var match = reg.Match(literalText);
            if (match.Success)
            {
                ShortGuid shortGuid;
                string splitOut = match.Value.Replace("guid_", string.Empty).Trim('\'');
                bool isShortGuid = ShortGuid.TryParse(splitOut, out shortGuid);
                if (isShortGuid)
                {
                    return $"'{shortGuid.Guid}'";
                }
                else
                {
                    return $"'{Guid.Empty}'";
                }
            }
            return literalText;
        }

        private void HandleSingleValueProperty(string realFieldName)
        {
            if (BuiltInContentTypeField.ReservedContentTypeFields.Any(p => p.DeveloperName == realFieldName))
            {
                whereClause.Append($" {RawSqlColumn.SOURCE_ITEM_COLUMN_NAME}.{realFieldName} ");
            }
            else
            {
                var chosenColumnAsCustomField = _contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == realFieldName);
                if (chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.OneToOneRelationship)
                {
                    var relatedObjectField = _relatedObjectFields.First(p => p.DeveloperName == chosenColumnAsCustomField.DeveloperName);
                    int indexOfRelatedObject = _relatedObjectFields.ToList().IndexOf(relatedObjectField);
                    string relatedObjPrimaryFieldName = relatedObjectField.ContentType.ContentTypeFields.First(p => p.Id == relatedObjectField.ContentType.PrimaryFieldId).DeveloperName;
                    whereClause.Append(chosenColumnAsCustomField.FieldType.SqlServerSingleJsonValue($"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{indexOfRelatedObject}", RawSqlColumn.PublishedContent.Name, relatedObjPrimaryFieldName));
                }
                else
                {
                    whereClause.Append(chosenColumnAsCustomField.FieldType.SqlServerSingleJsonValue(RawSqlColumn.SOURCE_ITEM_COLUMN_NAME, RawSqlColumn.PublishedContent.Name, realFieldName));
                }
            }
        }

        private void HandleVisitNextNode(QueryNode nextNode)
        {
            if (nextNode is BinaryOperatorNode)
            {
                this.Visit((BinaryOperatorNode)nextNode);
            }
            else if (nextNode is ConstantNode)
            {
                this.Visit((ConstantNode)nextNode);
            }
            else if (nextNode is SingleValuePropertyAccessNode)
            {
                this.Visit((SingleValuePropertyAccessNode)nextNode);
            }
            else if (nextNode is SingleValueOpenPropertyAccessNode)
            {
                var realFieldName = this.UseDeveloperNameForPrimaryField(((SingleValueOpenPropertyAccessNode)nextNode).Name);
                HandleSingleValueProperty(realFieldName);
            }
            else if (nextNode is ConvertNode)
            {
                this.Visit((ConvertNode)nextNode);
            }
            else if (nextNode is SingleValueFunctionCallNode)
            {
                this.Visit((SingleValueFunctionCallNode)nextNode);
            }
            else if (nextNode is UnaryOperatorNode)
            {
                this.Visit((UnaryOperatorNode)nextNode);
            }
            else
            {
                throw new NotImplementedException("Node not an implemented type: " + nextNode.Kind);
            }
        }
    }
}
