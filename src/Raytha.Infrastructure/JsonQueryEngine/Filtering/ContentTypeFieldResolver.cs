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
}

/// <summary>
/// A filter field after it has been checked against the content type. <see cref="ScalarSql"/> is the
/// expression used for comparisons and null checks (and the left side of a text match). Array fields
/// also carry <see cref="Alias"/>, <see cref="JsonColumn"/>, and <see cref="ArrayKey"/> so the
/// compiler can build their EXISTS predicates.
/// </summary>
internal sealed record ResolvedField(
    string Name,
    FilterFieldKind Kind,
    FilterValueType ValueType,
    string ScalarSql,
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
    private readonly string _dateFormat;

    private const string Source = RawSqlColumn.SOURCE_ITEM_COLUMN_NAME;

    private static readonly string JsonColumnName = RawSqlColumn.PublishedContent.Name;

    public ContentTypeFieldResolver(
        ContentType contentType,
        string primaryFieldDeveloperName,
        IEnumerable<ContentTypeField> relatedObjectFields,
        string dateFormat
    )
    {
        _contentType = contentType;
        _primaryFieldDeveloperName = primaryFieldDeveloperName;
        _relatedObjectFields = relatedObjectFields.ToList();
        _dateFormat = dateFormat;
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
        var valueType =
            realName == BuiltInContentTypeField.Id.DeveloperName
                ? FilterValueType.Guid
                : FilterValueType.Text;

        var isFilterableColumn =
            realName == BuiltInContentTypeField.Id.DeveloperName
            || realName == BuiltInContentTypeField.CreationTime.DeveloperName
            || realName == BuiltInContentTypeField.LastModificationTime.DeveloperName
            || realName == BuiltInContentTypeField.IsDraft.DeveloperName
            || realName == BuiltInContentTypeField.IsPublished.DeveloperName;

        if (!isFilterableColumn)
            throw new InvalidFilterException($"Field '{realName}' cannot be used in a filter.");

        return Scalar(
            realName,
            FilterFieldKind.ReservedColumn,
            valueType,
            PostgresFieldSql.ReservedColumn(Source, realName)
        );
    }

    private ResolvedField ResolveCustom(ContentTypeField field, string realName)
    {
        var typeName = field.FieldType.DeveloperName;

        if (typeName == BaseFieldType.Number)
            return Scalar(
                realName,
                FilterFieldKind.Number,
                FilterValueType.Number,
                PostgresFieldSql.NumberScalar(Source, JsonColumnName, realName)
            );
        if (typeName == BaseFieldType.Date)
            return Scalar(
                realName,
                FilterFieldKind.Date,
                FilterValueType.Text,
                PostgresFieldSql.DateScalar(Source, JsonColumnName, realName, _dateFormat)
            );
        if (typeName == BaseFieldType.MultipleSelect)
            return Array(realName, FilterFieldKind.MultiSelect);
        if (typeName == BaseFieldType.Repeater)
            return Array(realName, FilterFieldKind.Repeater);
        if (typeName == BaseFieldType.OneToOneRelationship)
            return ResolveRelationship(field, realName);

        return Scalar(
            realName,
            FilterFieldKind.Text,
            FilterValueType.Text,
            PostgresFieldSql.TextScalar(Source, JsonColumnName, realName)
        );
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
        return Scalar(
            realName,
            FilterFieldKind.Relationship,
            FilterValueType.Text,
            PostgresFieldSql.TextScalar(alias, JsonColumnName, relatedPrimaryFieldName)
        );
    }

    private static ResolvedField Scalar(
        string name,
        FilterFieldKind kind,
        FilterValueType valueType,
        string scalarSql
    ) => new(name, kind, valueType, scalarSql, string.Empty, string.Empty, string.Empty);

    private ResolvedField Array(string realName, FilterFieldKind kind) =>
        new(
            realName,
            kind,
            FilterValueType.Text,
            PostgresFieldSql.TextScalar(Source, JsonColumnName, realName),
            Source,
            JsonColumnName,
            realName
        );
}
