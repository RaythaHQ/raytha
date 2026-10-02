using Raytha.Application.Views;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentTypes;

/// <summary>
/// A site's content model as one portable document: every content type with its fields, choices,
/// and views. References between types use developer names, never ids, and lists keep a stable
/// order, so two exports of the same model are identical and a diff shows only real changes.
/// The same document is what an import reads.
/// </summary>
public record SchemaDocument
{
    public const int CurrentVersion = 1;

    public int SchemaVersion { get; init; } = CurrentVersion;
    public IReadOnlyList<SchemaContentType> ContentTypes { get; init; } = [];
}

public record SchemaContentType
{
    public string DeveloperName { get; init; } = string.Empty;
    public string LabelPlural { get; init; } = string.Empty;
    public string LabelSingular { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DefaultRouteTemplate { get; init; } = string.Empty;

    /// <summary>The developer name of the field that titles an item. Must be a single line text field.</summary>
    public string? PrimaryField { get; init; }

    /// <summary>In display order.</summary>
    public IReadOnlyList<SchemaField> Fields { get; init; } = [];
    public IReadOnlyList<SchemaView> Views { get; init; } = [];
}

public record SchemaField
{
    public string DeveloperName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>A field type developer name, for example <c>single_line_text</c>.</summary>
    public string FieldType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public IReadOnlyList<SchemaChoice> Choices { get; init; } = [];

    /// <summary>For a relationship field, the developer name of the content type it points at.</summary>
    public string? RelatedContentType { get; init; }
    public IReadOnlyList<FieldDefinition> SubFields { get; init; } = [];
}

public record SchemaChoice
{
    public string DeveloperName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool Disabled { get; init; }
}

public record SchemaView
{
    public string DeveloperName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsPublished { get; init; }

    /// <summary>The public URL path of the view. Left out on import to have one generated.</summary>
    public string? RoutePath { get; init; }

    /// <summary>The developer name of the list web template in the active theme.</summary>
    public string? Template { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = [];
    public IReadOnlyList<SchemaSort> Sort { get; init; } = [];
    public IReadOnlyList<FilterConditionInputDto> Filter { get; init; } = [];
    public int DefaultNumberOfItemsPerPage { get; init; } = View.DEFAULT_NUMBER_OF_ITEMS_PER_PAGE;
    public int MaxNumberOfItemsPerPage { get; init; } = View.DEFAULT_MAX_ITEMS_PER_PAGE;
    public bool IgnoreClientFilterAndSortQueryParams { get; init; }
}

public record SchemaSort
{
    public string DeveloperName { get; init; } = string.Empty;

    /// <summary><c>asc</c> or <c>desc</c>.</summary>
    public string Direction { get; init; } = "asc";
}
