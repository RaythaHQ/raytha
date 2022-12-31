using TimeZoneConverter;
using TimeZoneNames;

namespace Raytha.Application.Common.Utils;

public static class DateTimeExtensions
{
    public const string DEFAULT_TIMEZONE = "Etc/UTC";
    public const string DEFAULT_DATE_FORMAT = MM_dd_yyyy;

    public const string MM_dd_yyyy = "MM/dd/yyyy";
    public const string dd_MM_yyyy = "dd/MM/yyyy";

    public static IDictionary<string, string> GetTimeZoneDisplayNames()
    {
        return TZNames.GetDisplayNames("en-US", true);
    }

    public static DateTime? GetDateFromString(this string dateAsString)
    {
        if (string.IsNullOrEmpty(dateAsString))
            return null;

        return Convert.ToDateTime(dateAsString);
    }

    public static DateTime UtcToTimeZone(this DateTime utc, string timeZone)
    {
        TimeZoneInfo tzi = TZConvert.GetTimeZoneInfo(timeZone);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tzi);
    }

    public static DateTime TimeZoneToUtc(this DateTime currentDate, string timeZone)
    {
        TimeZoneInfo tzi = TZConvert.GetTimeZoneInfo(timeZone);
        return TimeZoneInfo.ConvertTimeToUtc(currentDate, tzi);
    }

    public static TimeZoneInfo GetTimeZoneInfo(string timeZone)
    {
        TimeZoneInfo tzi = TZConvert.GetTimeZoneInfo(timeZone);
        return tzi;
    }

    public static bool IsValidTimeZone(string timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return false;

        return DateTimeExtensions.GetTimeZoneDisplayNames().Select(p => p.Key).Contains(timeZone);
    }

    public static bool IsValidDateFormat(string dateFormat)
    {
        return GetDateFormats().Contains(dateFormat);
    }

    public static IEnumerable<string> GetDateFormats()
    {
        yield return MM_dd_yyyy;
        yield return dd_MM_yyyy;
    }
}
