using System;
using System.Diagnostics;
using System.Globalization;

namespace ConfigCat.Client.Utils;

internal static class DateTimeUtils
{
    public static long ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        // NOTE: Internally we should always work with UTC datetime values (as DateTimeKind.Unspecified can lead to incorrect results).
        Debug.Assert(dateTime.Kind == DateTimeKind.Utc, "Non-UTC datetime encountered.");

        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }

    public static string ToUnixTimeStamp(this DateTime dateTime)
    {
        return ToUnixTimeMilliseconds(dateTime).ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryConvertFromUnixTimeMilliseconds(long milliseconds, out DateTime dateTime)
    {
        try
        {
            dateTime = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            dateTime = default;
            return false;
        }
    }

    public static bool TryConvertFromUnixTimeSeconds(double seconds, out DateTime dateTime)
    {
        long milliseconds;
        try { milliseconds = checked((long)(seconds * 1000)); }
        catch (OverflowException)
        {
            dateTime = default;
            return false;
        }

        return TryConvertFromUnixTimeMilliseconds(milliseconds, out dateTime);
    }

    public static bool TryParseUnixTimeStamp(ReadOnlySpan<char> span, out DateTime dateTime)
    {
        if (!long.TryParse(span.ToParsable(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var milliseconds))
        {
            dateTime = default;
            return false;
        }

        return TryConvertFromUnixTimeMilliseconds(milliseconds, out dateTime);
    }

    public static TimeSpan GetMonotonicTime()
    {
        return TimeSpan.FromSeconds(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
    }
}
