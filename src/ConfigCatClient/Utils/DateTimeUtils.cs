using System;
using System.Globalization;

namespace ConfigCat.Client.Utils;

internal static class DateTimeUtils
{
    public static string ToUnixTimeStamp(this DateTime dateTime)
    {
#if !NET45
        var milliseconds = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
#else
        // Based on: https://github.com/dotnet/runtime/blob/v6.0.13/src/libraries/System.Private.CoreLib/src/System/DateTimeOffset.cs#L629

        const long unixEpochMilliseconds = 62_135_596_800_000L;
        var milliseconds = dateTime.Ticks / TimeSpan.TicksPerMillisecond - unixEpochMilliseconds;
#endif

        return milliseconds.ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryParseUnixTimeStamp(ReadOnlySpan<char> span, out DateTime dateTime)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        var slice = span;
#else
        var slice = span.ToString();
#endif

        if (!long.TryParse(slice, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var milliseconds))
        {
            dateTime = default;
            return false;
        }

#if !NET45
        try { dateTime = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime; }
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
#endif

        return true;
    }
}
