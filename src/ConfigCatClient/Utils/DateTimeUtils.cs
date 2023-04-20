using System;
using System.Globalization;

namespace ConfigCat.Client.Utils;

internal static class DateTimeUtils
{
    public static string ToHttpHeaderDate(this DateTime dateTime)
    {
        return dateTime.ToString("r", CultureInfo.InvariantCulture);
    }

    public static bool TryParseHttpHeaderDate(ReadOnlySpan<char> span, out DateTime dateTime)
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1
        var slice = span;
#else
        var slice = span.ToString();
#endif

        if (DateTime.TryParseExact(slice, "r", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
        {
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return true;
        }

        return false;
    }
}
