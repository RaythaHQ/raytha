namespace Raytha.Application.Common.Interfaces;

/// <summary>
/// Interface for retrieving raw database information.
/// </summary>
public interface IRaythaRawDbInfo
{
    /// <summary>
    /// Gets the current database size information.
    /// </summary>
    DbSpaceUsed GetDatabaseSize();
}

public class DbSpaceUsed
{
    public string database_name { get; init; }
    public string database_size { get; init; }
    public string reserved { get; init; }
    public string data { get; init; }
    public string index_size { get; init; }
}
