using System.Diagnostics.CodeAnalysis;

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

    public static bool IsValidOneOf([NotNullWhen(true)] object? field)
    {
        return field is not null && !ReferenceEquals(field, MultipleValuesToken);
    }
}
