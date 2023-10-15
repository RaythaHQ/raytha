namespace Raytha.Application.Common.Interfaces;

public interface ICsvService
{
    public IEnumerable<Dictionary<string, object>> ReadCsv<T>(Stream file);
}