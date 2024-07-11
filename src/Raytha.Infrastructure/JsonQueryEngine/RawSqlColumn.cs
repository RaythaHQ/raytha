namespace Raytha.Infrastructure.JsonQueryEngine;

internal class RawSqlColumn
{
    public const string UNIQUE_COLUMN_PREFIX = "raytha_cc";
    public const string SOURCE_ITEM_COLUMN_NAME = "source";
    public const string ROUTE_COLUMN_NAME = "route";
    public const string SOURCE_CREATED_BY_COLUMN_NAME = "created_by_source";
    public const string SOURCE_MODIFIED_BY_COLUMN_NAME = "modified_by_source";
    public const string RELATED_CREATED_BY_COLUMN_NAME = "created_by_related";
    public const string RELATED_MODIFIED_BY_COLUMN_NAME = "modified_by_related";
    public const string RELATED_ROUTE_COLUMN_NAME = "route_related";
    public const string RELATED_ITEM_COLUMN_NAME = "related";

    private RawSqlColumn()
    {
    }

    private RawSqlColumn(string name)
    {
        Name = name;
    }

    public static RawSqlColumn Id = new RawSqlColumn("Id");
    public static RawSqlColumn CreationTime = new RawSqlColumn("CreationTime");
    public static RawSqlColumn CreatorUserId = new RawSqlColumn("CreatorUserId");
    public static RawSqlColumn LastModifierUserId = new RawSqlColumn("LastModifierUserId");
    public static RawSqlColumn LastModificationTime = new RawSqlColumn("LastModificationTime");
    public static RawSqlColumn IsPublished = new RawSqlColumn("IsPublished");
    public static RawSqlColumn IsDraft = new RawSqlColumn("IsDraft");
    public static RawSqlColumn RouteId = new RawSqlColumn("RouteId");
    public static RawSqlColumn Path = new RawSqlColumn("Path");
    public static RawSqlColumn DraftContent = new RawSqlColumn("_DraftContent");
    public static RawSqlColumn PublishedContent = new RawSqlColumn("_PublishedContent");
    public static RawSqlColumn ContentTypeId = new RawSqlColumn("ContentTypeId");
    public static RawSqlColumn FirstName = new RawSqlColumn("FirstName");
    public static RawSqlColumn LastName = new RawSqlColumn("LastName");
    public static RawSqlColumn EmailAddress = new RawSqlColumn("EmailAddress");
    public static RawSqlColumn Label = new RawSqlColumn("Label");
    public static RawSqlColumn DeveloperName = new RawSqlColumn("DeveloperName");

    public string Name { get; private set; }

    public string NameAsColumn(string columnPrefix)
    {
        return $"{columnPrefix}.{Name}";
    }
    public string NameAsColumnLabel(string columnPrefix)
    {
        return $"{columnPrefix}_{Name}";
    }
    public string NameAsFullColumnLabel(string columnPrefix)
    {
        return $"{NameAsColumn(columnPrefix)} as {NameAsColumnLabel(columnPrefix)}";
    }

    public static IEnumerable<string> NameAsFullColumnLabelForEnumerable(IEnumerable<RawSqlColumn> columns, string columnPrefix)
    {
        foreach (var item in columns)
        {
            yield return item.NameAsFullColumnLabel(columnPrefix);
        }
    }
    public static IEnumerable<RawSqlColumn> ContentItemColumns()
    {
        yield return Id;
        yield return CreationTime;
        yield return CreatorUserId;
        yield return LastModifierUserId;
        yield return LastModificationTime;
        yield return IsPublished;
        yield return IsDraft;
        yield return DraftContent;
        yield return PublishedContent;
        yield return ContentTypeId;
    }

    public static IEnumerable<RawSqlColumn> UserColumns()
    {
        yield return Id;
        yield return FirstName;
        yield return LastName;
        yield return EmailAddress;
    }

    public static IEnumerable<RawSqlColumn> TemplateColumns()
    {
        yield return Id;
        yield return Label;
        yield return DeveloperName;
    }

    public static IEnumerable<RawSqlColumn> RouteColumns()
    {
        yield return Id;
        yield return Path;
    }
}