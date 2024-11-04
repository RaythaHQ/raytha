namespace Raytha.Application.Common.Utils;

public static class DictionaryExtensions
{
    public static Dictionary<string, TValue> FilterAndTrimKeys<TValue>(this IDictionary<string, TValue> source, string prefix)
    {
        return source
            .Where(pair => pair.Key.StartsWith(prefix))
            .ToDictionary(
                pair => pair.Key.Substring(prefix.Length), 
                pair => pair.Value
            );
    }
}
