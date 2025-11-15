namespace Raytha.Application.Common.Utils;

public class OrganizationTimeZoneConverter
{
    private const string TIME_FORMAT = "h:mm:ss tt";

    private OrganizationTimeZoneConverter(string timezone, string dateTimeFormat)
    {
        TimeZone = timezone;
        DateFormat = dateTimeFormat;
        DateTimeFormat = $"{dateTimeFormat} {TIME_FORMAT}";
    }

    public string TimeZone { get; private set; }
    public string DateTimeFormat { get; private set; }
    public string DateFormat { get; private set; }

    public static OrganizationTimeZoneConverter From(string timezone, string dateTimeFormat)
    {
        return new OrganizationTimeZoneConverter(timezone, dateTimeFormat);
    }

    public static OrganizationTimeZoneConverter Default =>
        new OrganizationTimeZoneConverter(
            DateTimeExtensions.DEFAULT_TIMEZONE,
            DateTimeExtensions.DEFAULT_DATE_FORMAT
        );

    public DateTime UtcToTimeZone(DateTime utc)
    {
        return utc.UtcToTimeZone(TimeZone);
    }

    public string ToDateTimeFormat(DateTime? date)
    {
        return date.HasValue ? date.Value.ToString(DateTimeFormat) : "Never";
    }

    public string UtcToTimeZoneAsDateTimeFormat(DateTime? utc)
    {
        return utc.HasValue ? utc.Value.UtcToTimeZone(TimeZone).ToString(DateTimeFormat) : "Never";
    }

    public string ToDateFormat(DateTime? date)
    {
        return date.HasValue ? date.Value.ToString(DateFormat) : "Never";
    }

    public string UtcToTimeZoneAsDateFormat(DateTime? utc)
    {
        return utc.HasValue ? utc.Value.UtcToTimeZone(TimeZone).ToString(DateFormat) : "Never";
    }
}
