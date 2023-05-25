using System;
using System.Globalization;

namespace ConfigCat.Client.Utils;

internal static class DateTimeUtils
{
    public static string ToUnixTimeStamp(this DateTime dateTime)
    {
#if !NET45
        var seconds = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
#else
        // Based on: https://github.com/dotnet/runtime/blob/v6.0.13/src/libraries/System.Private.CoreLib/src/System/DateTimeOffset.cs#L607

        const long unixEpochSeconds = 62_135_596_800L;

        var seconds = dateTime.Ticks / TimeSpan.TicksPerSecond - unixEpochSeconds;
#endif

        return seconds.ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryParseUnixTimeStamp(ReadOnlySpan<char> span, out DateTime dateTime)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        var slice = span;
#else
        var slice = span.ToString();
#endif

        if (!long.TryParse(slice, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var seconds))
        {
            dateTime = default;
            return false;
        }

#if !NET45
        try { dateTime = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime; }
        catch (ArgumentOutOfRangeException)
        {
            dateTime = default;
            return false;
        }
#else
        // Based on: https://github.com/dotnet/runtime/blob/v6.0.13/src/libraries/System.Private.CoreLib/src/System/DateTimeOffset.cs#L431

        const long unixEpochSeconds = 62_135_596_800L;
        const long unixMinSeconds = 0 / TimeSpan.TicksPerSecond - unixEpochSeconds;
        const long unixMaxSeconds = 3_155_378_975_999_999_999L / TimeSpan.TicksPerSecond - unixEpochSeconds;

        if (seconds < unixMinSeconds || seconds > unixMaxSeconds)
        {
            dateTime = default;
            return false;
        }

        long ticks = (seconds + unixEpochSeconds) * TimeSpan.TicksPerSecond;
        dateTime = new DateTime(ticks, DateTimeKind.Utc);
#endif

        return true;
    }
}
