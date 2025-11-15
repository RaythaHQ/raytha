using System.Text;

namespace Raytha.Infrastructure.JsonQueryEngine;

internal class SqlQueryBuilder
{
    private readonly List<string> _selectClauses = new();
    private readonly StringBuilder _fromClause = new();
    private readonly List<string> _whereClauses = new();
    private readonly List<string> _orderByClauses = new();
    private readonly List<string> _joinClauses = new();

    public SqlQueryBuilder Select(params string[] columns)
    {
        _selectClauses.Add(columns.Length > 0 ? string.Join(", ", columns) : "*");
        return this;
    }

    public SqlQueryBuilder From(string table)
    {
        _fromClause.Append($"FROM {table}");
        return this;
    }

    public SqlQueryBuilder AndWhere(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return this;

        if (!_whereClauses.Any())
            _whereClauses.Add($"{condition}");
        else
            _whereClauses.Add($" AND {condition}");

        return this;
    }

    public SqlQueryBuilder OrWhere(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return this;

        if (!_whereClauses.Any())
            _whereClauses.Add($"{condition}");
        else
            _whereClauses.Add($" OR {condition}");

        return this;
    }

    public SqlQueryBuilder OrderBy(string column)
    {
        if (string.IsNullOrWhiteSpace(column))
            return this;

        _orderByClauses.Add(column);
        return this;
    }

    public SqlQueryBuilder Join(string table, string onCondition, string joinType = "INNER")
    {
        _joinClauses.Add($"{joinType} JOIN {table} ON {onCondition}");
        return this;
    }

    public string Build()
    {
        var query = new StringBuilder();

        if (_selectClauses.Any())
        {
            query.Append("SELECT ");
            query.Append(string.Join(", ", _selectClauses));
            query.Append(" ");
        }

        if (_fromClause.Length > 0)
            query.Append(_fromClause).Append(" ");

        if (_joinClauses.Any())
        {
            query.Append(string.Join(" ", _joinClauses)).Append(" ");
        }

        if (_whereClauses.Any())
        {
            query.Append("WHERE ");
            query.Append(string.Join(" ", _whereClauses)).Append(" ");
        }

        if (_orderByClauses.Any())
        {
            query.Append("ORDER BY ");
            query.Append(string.Join(", ", _orderByClauses));
        }

        return query.ToString().TrimEnd();
    }
}
