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

#if !NET45
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
#else
        // Based on: https://github.com/dotnet/runtime/blob/v6.0.13/src/libraries/System.Private.CoreLib/src/System/DateTimeOffset.cs#L629

        const long unixEpochMilliseconds = 62_135_596_800_000L;
        return dateTime.Ticks / TimeSpan.TicksPerMillisecond - unixEpochMilliseconds;
#endif
    }

    public static string ToUnixTimeStamp(this DateTime dateTime)
    {
        return ToUnixTimeMilliseconds(dateTime).ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryConvertFromUnixTimeMilliseconds(long milliseconds, out DateTime dateTime)
    {
#if !NET45
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
#else
        // Based on: https://github.com/dotnet/runtime/blob/v6.0.13/src/libraries/System.Private.CoreLib/src/System/DateTimeOffset.cs#L443

        const long unixEpochMilliseconds = 62_135_596_800_000L;
        const long unixMinMilliseconds = 0 / TimeSpan.TicksPerMillisecond - unixEpochMilliseconds;
        const long unixMaxMilliseconds = 3_155_378_975_999_999_999L / TimeSpan.TicksPerMillisecond - unixEpochMilliseconds;

        if (milliseconds < unixMinMilliseconds || milliseconds > unixMaxMilliseconds)
        {
            dateTime = default;
            return false;
        }

        var ticks = (milliseconds + unixEpochMilliseconds) * TimeSpan.TicksPerMillisecond;
        dateTime = new DateTime(ticks, DateTimeKind.Utc);
        return true;
#endif
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
#if NET
        return TimeSpan.FromMilliseconds(Environment.TickCount64);
#else
        return TimeSpan.FromSeconds(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
#endif
    }
}
