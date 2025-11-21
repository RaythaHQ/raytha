using System.Text;

namespace Raytha.Application.Common.Utils;

/// <summary>
/// Utility for generating properly formatted CSV files without additional metadata rows.
/// </summary>
public class CsvWriterUtility
{
    private readonly List<string> _headers = new();
    private readonly List<Dictionary<string, string>> _rows = new();

    /// <summary>
    /// Adds a row to the CSV. The first row added will determine the headers.
    /// </summary>
    /// <param name="rowData">Dictionary mapping column names to values</param>
    public void AddRow(Dictionary<string, string> rowData)
    {
        if (!_headers.Any())
        {
            _headers.AddRange(rowData.Keys);
        }
        _rows.Add(new Dictionary<string, string>(rowData));
    }

    /// <summary>
    /// Adds a row with specific columns. Columns must match existing headers.
    /// </summary>
    /// <param name="columns">Ordered list of column names</param>
    /// <param name="values">Ordered list of values matching the columns</param>
    public void AddRow(IEnumerable<string> columns, IEnumerable<string> values)
    {
        var columnsList = columns.ToList();
        var valuesList = values.ToList();

        if (columnsList.Count != valuesList.Count)
        {
            throw new ArgumentException("Number of columns must match number of values");
        }

        if (!_headers.Any())
        {
            _headers.AddRange(columnsList);
        }

        var rowData = new Dictionary<string, string>();
        for (int i = 0; i < columnsList.Count; i++)
        {
            rowData[columnsList[i]] = valuesList[i];
        }
        _rows.Add(rowData);
    }

    /// <summary>
    /// Exports the CSV data as a byte array with UTF-8 encoding and BOM for Excel compatibility.
    /// </summary>
    /// <returns>CSV content as byte array</returns>
    public byte[] ExportToBytes()
    {
        var csvContent = ExportToString();
        // Add UTF-8 BOM for proper Excel compatibility without needing "sep="
        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(csvContent);
        var result = new byte[preamble.Length + contentBytes.Length];
        preamble.CopyTo(result, 0);
        contentBytes.CopyTo(result, preamble.Length);
        return result;
    }

    /// <summary>
    /// Exports the CSV data as a string.
    /// </summary>
    /// <returns>CSV content as string</returns>
    public string ExportToString()
    {
        var sb = new StringBuilder();

        // Write headers
        sb.AppendLine(string.Join(",", _headers.Select(h => EscapeCsvValue(h))));

        // Write rows
        foreach (var row in _rows)
        {
            var values = _headers.Select(header =>
                row.ContainsKey(header) ? EscapeCsvValue(row[header]) : string.Empty
            );
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a value for CSV format according to RFC 4180.
    /// Values containing commas, quotes, or newlines are enclosed in quotes.
    /// Quotes within values are escaped by doubling them.
    /// </summary>
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Check if value needs quoting
        bool needsQuoting = value.Contains(',') || 
                           value.Contains('"') || 
                           value.Contains('\n') || 
                           value.Contains('\r');

        if (needsQuoting)
        {
            // Escape quotes by doubling them
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    /// <summary>
    /// Clears all data from the writer.
    /// </summary>
    public void Clear()
    {
        _headers.Clear();
        _rows.Clear();
    }

    /// <summary>
    /// Gets the number of rows (excluding header).
    /// </summary>
    public int RowCount => _rows.Count;

    /// <summary>
    /// Gets the column headers.
    /// </summary>
    public IReadOnlyList<string> Headers => _headers.AsReadOnly();
}

