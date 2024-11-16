using CSharpVitamins;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Raytha.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace Raytha.Infrastructure.JsonQueryEngine;

internal abstract class AbstractODataFilterToSql
{
    protected ContentType _contentType;
    protected string _primaryFieldName;
    protected IEnumerable<ContentTypeField> _relatedObjectFields;
    protected string _dateTimeFormat;

    protected string placeholderClassName = typeof(PlaceholderClass).Name;

    protected IEdmModel GetEdmModel()
    {
        ODataModelBuilder builder = new ODataConventionModelBuilder();
        builder.EntitySet<PlaceholderClass>(placeholderClassName);
        return builder.GetEdmModel();
    }

    protected class PlaceholderClass
    {
        [Key]
        public int Id { get; set; }
        public IDictionary<string, object> Properties { get; set; }
    }

    public abstract string GenerateSql(string filterExpression);

    protected abstract class AbstractODataFilterToSqlVisitor<TSource> : QueryNodeVisitor<TSource> where TSource : class
    {
        protected StringBuilder whereClause = new StringBuilder();
        protected ContentType _contentType;
        protected string _primaryFieldName;
        protected string _dateTimeFormat;
        protected IEnumerable<ContentTypeField> _relatedObjectFields;

        protected abstract void HandleSingleValueProperty(string realFieldName);

        public virtual string GetWhereClause()
        {
            return whereClause.ToString()
                .Replace("!= null", "IS NOT NULL")
                .Replace("= null", "IS NULL");
        }

        public override TSource Visit(UnaryOperatorNode nodeIn)
        {
            if (nodeIn.OperatorKind == UnaryOperatorKind.Not)
                whereClause.Append(" NOT ");

            var nextNode = nodeIn.Operand;
            HandleVisitNextNode(nextNode);

            return null;
        }

        public override TSource Visit(ConvertNode nodeIn)
        {
            HandleVisitNextNode(nodeIn.Source);
            return null;
        }

        public override TSource Visit(SingleValuePropertyAccessNode nodeIn)
        {
            var realFieldName = UseDeveloperNameForPrimaryField(nodeIn.Property.Name);
            HandleSingleValueProperty(realFieldName);
            return null;
        }

        public override TSource Visit(ConstantNode nodeIn)
        {
            string literalText = TryParseGuidOrValue(nodeIn.LiteralText);
            whereClause.Append(literalText);
            return null;
        }

        public override TSource Visit(BinaryOperatorNode nodeIn)
        {
            whereClause.Append("(");
            var bon = nodeIn;
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

        protected virtual void HandleVisitNextNode(QueryNode nextNode)
        {
            if (nextNode is BinaryOperatorNode)
            {
                Visit((BinaryOperatorNode)nextNode);
            }
            else if (nextNode is ConstantNode)
            {
                Visit((ConstantNode)nextNode);
            }
            else if (nextNode is SingleValuePropertyAccessNode)
            {
                Visit((SingleValuePropertyAccessNode)nextNode);
            }
            else if (nextNode is SingleValueOpenPropertyAccessNode)
            {
                var realFieldName = UseDeveloperNameForPrimaryField(((SingleValueOpenPropertyAccessNode)nextNode).Name);
                HandleSingleValueProperty(realFieldName);
            }
            else if (nextNode is ConvertNode)
            {
                Visit((ConvertNode)nextNode);
            }
            else if (nextNode is SingleValueFunctionCallNode)
            {
                Visit((SingleValueFunctionCallNode)nextNode);
            }
            else if (nextNode is UnaryOperatorNode)
            {
                Visit((UnaryOperatorNode)nextNode);
            }
            else
            {
                throw new NotImplementedException("Node not an implemented type: " + nextNode.Kind);
            }
        }

        protected string UseDeveloperNameForPrimaryField(string name)
        {
            if (name == BuiltInContentTypeField.PrimaryField)
            {
                return _primaryFieldName;
            }
            return name;
        }

        protected string TryParseGuidOrValue(string literalText)
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
    }
}
