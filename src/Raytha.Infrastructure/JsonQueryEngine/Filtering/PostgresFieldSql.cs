namespace Raytha.Infrastructure.JsonQueryEngine.Filtering;

/// <summary>
/// Postgres column and match expressions for content-item fields. These build only from column
/// identifiers the resolver has already validated against the content type; user values never
/// appear here and are always bound as parameters by the compiler.
/// </summary>
internal static class PostgresFieldSql
{
    public static string TextScalar(string alias, string jsonColumn, string key) =>
        $"COALESCE({alias}.\"{jsonColumn}\"->>'{key}', '')";

    public static string NumberScalar(string alias, string jsonColumn, string key) =>
        $"CASE WHEN ({alias}.\"{jsonColumn}\"->>'{key}') ~ '^[0-9]+(\\.[0-9]+)?$' "
        + $"THEN ({alias}.\"{jsonColumn}\"->> '{key}')::decimal(18, 2) ELSE NULL END";

    /// <summary>
    /// A stored date as <c>date</c>. The admin SPA and API clients store ISO dates; the Razor admin
    /// stored the organization's format, so both are read.
    /// </summary>
    public static string DateScalar(string alias, string jsonColumn, string key, string dateFormat) =>
        $"CASE WHEN ({alias}.\"{jsonColumn}\"->>'{key}') ~ '^[0-9]{{4}}-[0-9]{{2}}-[0-9]{{2}}' "
        + $"THEN TO_DATE(LEFT({alias}.\"{jsonColumn}\"->>'{key}', 10), 'YYYY-MM-DD') "
        + $"ELSE TO_DATE(NULLIF({alias}.\"{jsonColumn}\"->>'{key}', ''), '{Format(dateFormat)}') END";

    public static string ReservedColumn(string alias, string columnName) =>
        $"{alias}.\"{columnName}\"";

    /// <summary>
    /// A relationship value as <c>uuid</c>, or NULL when the stored text is not a hyphenated Guid.
    /// A bare <c>::uuid</c> cast would fail the whole list query on one malformed row.
    /// </summary>
    public static string RelationshipId(string alias, string jsonColumn, string key) =>
        $"CASE WHEN ({alias}.\"{jsonColumn}\"->>'{key}') ~* "
        + "'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' "
        + $"THEN ({alias}.\"{jsonColumn}\"->>'{key}')::uuid ELSE NULL END";

    public static string MultiSelectEmpty(string alias, string jsonColumn, string key) =>
        $"(({alias}.\"{jsonColumn}\"->'{key}') IS NULL OR NOT EXISTS "
        + $"(SELECT 1 FROM jsonb_array_elements_text({alias}.\"{jsonColumn}\"->'{key}')))";

    public static string MultiSelectContains(
        string alias,
        string jsonColumn,
        string key,
        string parameter
    ) =>
        $"(({alias}.\"{jsonColumn}\"->'{key}') IS NOT NULL AND EXISTS "
        + $"(SELECT 1 FROM jsonb_array_elements_text({alias}.\"{jsonColumn}\"->'{key}') AS item "
        + $"WHERE item = {parameter}))";

    public static string RepeaterEmpty(string alias, string jsonColumn, string key) =>
        $"(COALESCE({alias}.\"{jsonColumn}\"->'{key}', '[]'::jsonb) "
        + "IN ('[]'::jsonb, 'null'::jsonb, '\"\"'::jsonb))";

    public static string TextOrderBy(string alias, string jsonColumn, string key, string direction) =>
        $"COALESCE({alias}.\"{jsonColumn}\"->>'{key}', '') {direction}";

    public static string NumberOrderBy(string alias, string jsonColumn, string key, string direction) =>
        $"CASE WHEN ({alias}.\"{jsonColumn}\"->>'{key}') ~ '^[0-9]+(\\.[0-9]+)?$' "
        + $"THEN ({alias}.\"{jsonColumn}\"->> '{key}')::decimal ELSE NULL END {direction}, "
        + $"{alias}.\"{jsonColumn}\"->>'{key}' {direction}";

    public static string DateOrderBy(
        string alias,
        string jsonColumn,
        string key,
        string dateFormat,
        string direction
    ) =>
        $"{DateScalar(alias, jsonColumn, key, dateFormat)} "
        + $"{(direction.ToUpperInvariant() == "DESC" ? "DESC" : "ASC")}";

    public static string MultiSelectOrderBy(
        string alias,
        string jsonColumn,
        string key,
        string direction
    ) =>
        $"COALESCE((SELECT value from jsonb_array_elements_text({alias}.\"{jsonColumn}\"->'{key}') "
        + $"AS value LIMIT 1), '') {direction}";

    public static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public static string WrapLike(MatchKind kind, string escaped) =>
        kind switch
        {
            MatchKind.StartsWith => $"{escaped}%",
            MatchKind.EndsWith => $"%{escaped}",
            _ => $"%{escaped}%",
        };

    private static string Format(string dateFormat) => (dateFormat ?? string.Empty).ToUpperInvariant();
}
