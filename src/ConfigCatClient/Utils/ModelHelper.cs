using System;

namespace ConfigCat.Client.Utils;

internal static class ModelHelper
{
    private static readonly object MultipleValuesToken = new();

    public static void SetOneOf<T>(ref object? field, T? value)
    {
        if (value is not null)
        {
            field = field is null ? value : MultipleValuesToken;
        }
    }

    public static bool IsValidOneOf(object? field)
    {
        return field is not null && !ReferenceEquals(field, MultipleValuesToken);
    }

    public static void SetEnum<TEnum>(ref TEnum field, TEnum value) where TEnum : struct, Enum
    {
        // NOTE: System.Text.Json throws when it encounters an undefined enum value but Newtonsoft.Json doesn't.
        // It just sets the property to the undefined numeric value. Unfortunately, there's no simple solution to this.
        // Multiple workarounds exist, probably this is the lesser evil: https://github.com/dotnet/runtime/issues/42093#issuecomment-692276834
        // TODO: get rid of the workaround when we drop support for .NET 4.5.

        field =
#if NET5_0_OR_GREATER
            Enum.IsDefined(value)
#else
            Enum.IsDefined(typeof(TEnum), value)
#endif
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, null);
    }
}
