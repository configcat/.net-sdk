using ConfigCat.Client.Evaluation;

namespace System
{
    internal static class TypeExtensions
    {
        public static void EnsureSupportedSettingClrType(this Type type)
        {
            type.ToSettingType().EnsureSupportedSettingType(isAnyAllowed: type == typeof(object));
        }

        public static void EnsureSupportedSettingType(this SettingType type, bool isAnyAllowed)
        {
            if (!isAnyAllowed && type == SettingType.Unknown)
            {
                throw new InvalidOperationException($"Only {typeof(string)}, {typeof(bool)}, {typeof(int)}, {typeof(long)}, {typeof(double)} and {typeof(object)} are supported.");
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
                    SettingType.Unknown,
            };
        }
    }
}
