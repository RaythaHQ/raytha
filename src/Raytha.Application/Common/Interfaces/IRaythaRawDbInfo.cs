namespace Raytha.Application.Common.Interfaces;

public interface IRaythaRawDbInfo
{
    DbSpaceUsed GetDatabaseSize();
}

public class DbSpaceUsed
{
    public string database_name { get; init; }
    public string database_size { get; init; }
    public string data { get; init; }
    public string index_size { get; init; }
}