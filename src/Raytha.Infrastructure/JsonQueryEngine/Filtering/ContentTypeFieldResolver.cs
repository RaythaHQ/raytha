using Raytha.Application.Common.Exceptions;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Infrastructure.JsonQueryEngine.Filtering;

internal enum FilterFieldKind
{
    ReservedColumn,
    Text,
    Number,
    Date,
    Relationship,
    MultiSelect,
    Repeater,
}

internal enum FilterValueType
{
    Text,
    Number,
    Guid,
    Boolean,
    Timestamp,
    Date,
}

/// <summary>
/// A filter field after it has been checked against the content type. <see cref="ScalarSql"/> is the
/// typed expression used for comparisons and null checks; <see cref="TextSql"/> is the stored text
/// that contains/startswith/endswith match against. Array fields also carry <see cref="Alias"/>,
/// <see cref="JsonColumn"/>, and <see cref="ArrayKey"/> so the compiler can build their EXISTS
/// predicates.
/// </summary>
internal sealed record ResolvedField(
    string Name,
    FilterFieldKind Kind,
    FilterValueType ValueType,
    string ScalarSql,
    string TextSql,
    string Alias,
    string JsonColumn,
    string ArrayKey
);

/// <summary>
/// Maps a filter field name to a <see cref="ResolvedField"/> using the content type's own fields.
/// A name that is not a supported reserved column or a declared custom field is rejected as an
/// <see cref="InvalidFilterException"/>, so an attacker-chosen identifier never reaches SQL.
/// </summary>
internal sealed class ContentTypeFieldResolver
{
    private readonly ContentType _contentType;
    private readonly string _primaryFieldDeveloperName;
    private readonly List<ContentTypeField> _relatedObjectFields;

    private const string Source = RawSqlColumn.SOURCE_ITEM_COLUMN_NAME;

    private static readonly string JsonColumnName = RawSqlColumn.PublishedContent.Name;

    public ContentTypeFieldResolver(
        ContentType contentType,
        string primaryFieldDeveloperName,
        IEnumerable<ContentTypeField> relatedObjectFields
    )
    {
        _contentType = contentType;
        _primaryFieldDeveloperName = primaryFieldDeveloperName;
        _relatedObjectFields = relatedObjectFields.ToList();
    }

    public ResolvedField Resolve(string name)
    {
        var realName =
            name == BuiltInContentTypeField.PrimaryField.DeveloperName
                ? _primaryFieldDeveloperName
                : name;

        if (BuiltInContentTypeField.ReservedContentTypeFields.Any(p => p.DeveloperName == realName))
            return ResolveReserved(realName);

        var field = _contentType.ContentTypeFields.FirstOrDefault(p => p.DeveloperName == realName);
        if (field == null)
            throw new InvalidFilterException($"Unknown filter field '{name}'.");

        return ResolveCustom(field, realName);
    }

    private ResolvedField ResolveReserved(string realName)
    {
        FilterValueType valueType;
        if (realName == BuiltInContentTypeField.Id.DeveloperName)
            valueType = FilterValueType.Guid;
        else if (
            realName == BuiltInContentTypeField.CreationTime.DeveloperName
            || realName == BuiltInContentTypeField.LastModificationTime.DeveloperName
        )
            valueType = FilterValueType.Timestamp;
        else if (
            realName == BuiltInContentTypeField.IsDraft.DeveloperName
            || realName == BuiltInContentTypeField.IsPublished.DeveloperName
        )
            valueType = FilterValueType.Boolean;
        else
            throw new InvalidFilterException($"Field '{realName}' cannot be used in a filter.");

        var column = PostgresFieldSql.ReservedColumn(Source, realName);
        return Scalar(realName, FilterFieldKind.ReservedColumn, valueType, column, column);
    }

    private ResolvedField ResolveCustom(ContentTypeField field, string realName)
    {
        var typeName = field.FieldType.DeveloperName;
        var storedText = PostgresFieldSql.TextScalar(Source, JsonColumnName, realName);

        if (typeName == BaseFieldType.Number)
            return Scalar(
                realName,
                FilterFieldKind.Number,
                FilterValueType.Number,
                PostgresFieldSql.NumberScalar(Source, JsonColumnName, realName),
                storedText
            );
        if (typeName == BaseFieldType.Date)
            return Scalar(
                realName,
                FilterFieldKind.Date,
                FilterValueType.Date,
                PostgresFieldSql.DateScalar(Source, JsonColumnName, realName),
                storedText
            );
        if (typeName == BaseFieldType.MultipleSelect)
            return Array(realName, FilterFieldKind.MultiSelect);
        if (typeName == BaseFieldType.Repeater)
            return Array(realName, FilterFieldKind.Repeater);
        if (typeName == BaseFieldType.OneToOneRelationship)
            return ResolveRelationship(field, realName);

        return Scalar(realName, FilterFieldKind.Text, FilterValueType.Text, storedText, storedText);
    }

    private ResolvedField ResolveRelationship(ContentTypeField field, string realName)
    {
        var relatedObjectField = _relatedObjectFields.FirstOrDefault(p =>
            p.DeveloperName == field.DeveloperName
        );
        if (relatedObjectField == null)
            throw new InvalidFilterException($"Field '{realName}' cannot be used in a filter.");

        var index = _relatedObjectFields.IndexOf(relatedObjectField);
        var relatedPrimaryFieldName = relatedObjectField
            .ContentType.ContentTypeFields.First(p =>
                p.Id == relatedObjectField.ContentType.PrimaryFieldId
            )
            .DeveloperName;

        var alias = $"{RawSqlColumn.RELATED_ITEM_COLUMN_NAME}_{index}";
        var relatedPrimary = PostgresFieldSql.TextScalar(alias, JsonColumnName, relatedPrimaryFieldName);
        return new ResolvedField(
            realName,
            FilterFieldKind.Relationship,
            FilterValueType.Text,
            relatedPrimary,
            relatedPrimary,
            alias,
            string.Empty,
            string.Empty
        );
    }

    private static ResolvedField Scalar(
        string name,
        FilterFieldKind kind,
        FilterValueType valueType,
        string scalarSql,
        string textSql
    ) => new(name, kind, valueType, scalarSql, textSql, string.Empty, string.Empty, string.Empty);

    private ResolvedField Array(string realName, FilterFieldKind kind)
    {
        var storedText = PostgresFieldSql.TextScalar(Source, JsonColumnName, realName);
        return new(
            realName,
            kind,
            FilterValueType.Text,
            storedText,
            storedText,
            Source,
            JsonColumnName,
            realName
        );
    }
}
