using ConfigCat.Client;

namespace System;

internal static class TypeExtensions
{
    public static void EnsureSupportedSettingClrType(this Type type, string paramName)
    {
        if (type != typeof(object) && type.ToSettingType() == Setting.UnknownType)
        {
            throw new ArgumentException($"Only the following types are supported: {typeof(string)}, {typeof(bool)}, {typeof(int)}, {typeof(long)}, {typeof(double)} and {typeof(object)} (both nullable and non-nullable).", paramName);
        }
    }

    public static SettingType ToSettingType(this Type type)
    {
        if (type.IsValueType && Nullable.GetUnderlyingType(type) is { } underlyingType)
        {
            type = underlyingType;
        }

        return Type.GetTypeCode(type) switch
        {
            TypeCode.String =>
                SettingType.String,
            TypeCode.Boolean =>
                SettingType.Boolean,
            TypeCode.Int32 or
            TypeCode.Int64 =>
                SettingType.Int,
            TypeCode.Double =>
                SettingType.Double,
            _ =>
                Setting.UnknownType,
        };
    }
}
