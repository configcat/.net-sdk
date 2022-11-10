using System.Diagnostics;
using System.Globalization;
using ConfigCat.Client.Evaluation;

#if USE_NEWTONSOFT_JSON
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace System
{
    internal static class ObjectExtensions
    {
        private static bool IsWithinAllowedIntRange(IConvertible value)
        {
            // Range of Int setting types: "any whole number within the range of Int32"
            // (https://configcat.com/docs/main-concepts/#about-setting-types)

            return value.GetTypeCode() switch
            {
                TypeCode.SByte or
                TypeCode.Byte or
                TypeCode.Int16 or
                TypeCode.UInt16 or
                TypeCode.Int32 =>
                    true,
                TypeCode.UInt32 =>
                    value.ToUInt32(CultureInfo.InvariantCulture) is <= int.MaxValue,
                TypeCode.Int64 =>
                    value.ToInt64(CultureInfo.InvariantCulture) is >= int.MinValue and <= int.MaxValue,
                TypeCode.UInt64 =>
                    value.ToUInt64(CultureInfo.InvariantCulture) is <= int.MaxValue,
                _ =>
                    false,
            };
        }

        private static bool IsWithinAllowedDoubleRange(IConvertible value)
        {
            // Range of Double setting types: "any decimal number within the range of double"
            // (https://configcat.com/docs/main-concepts/#about-setting-types)

            return value.GetTypeCode() is TypeCode.Single or TypeCode.Double;
        }

        public static SettingType DetermineSettingType(this JsonValue value)
        {
#if USE_NEWTONSOFT_JSON
            return value.Type switch
            {
                Newtonsoft.Json.Linq.JTokenType.String =>
                    SettingType.String,
                Newtonsoft.Json.Linq.JTokenType.Boolean =>
                    SettingType.Boolean,
                Newtonsoft.Json.Linq.JTokenType.Integer when IsWithinAllowedIntRange(value) =>
                    SettingType.Int,
                Newtonsoft.Json.Linq.JTokenType.Float when IsWithinAllowedDoubleRange(value) =>
                    SettingType.Double,
                _ =>
                    SettingType.Unknown,
            };
#else
            return value.ValueKind switch
            {
                Text.Json.JsonValueKind.String =>
                    SettingType.String,
                Text.Json.JsonValueKind.False or
                Text.Json.JsonValueKind.True =>
                    SettingType.Boolean,
                Text.Json.JsonValueKind.Number when value.TryGetInt32(out var _) =>
                    SettingType.Int,
                Text.Json.JsonValueKind.Number when value.TryGetDouble(out var _) =>
                    SettingType.Double,
                _ =>
                    SettingType.Unknown,
            };
#endif
        }

        public static SettingType DetermineSettingType(this object value)
        {
            if (value is null)
            {
                return SettingType.Unknown;
            }

            if (value is JsonValue jsonValue)
            {
                return jsonValue.DetermineSettingType();
            }

            return Type.GetTypeCode(value.GetType()) switch
            {
                TypeCode.String =>
                    SettingType.String,
                TypeCode.Boolean =>
                    SettingType.Boolean,
                TypeCode.SByte or
                TypeCode.Byte or
                TypeCode.Int16 or
                TypeCode.UInt16 or
                TypeCode.Int32 or
                TypeCode.UInt32 or
                TypeCode.Int64 or
                TypeCode.UInt64 when IsWithinAllowedIntRange((IConvertible)value) =>
                    SettingType.Int,
                TypeCode.Single or
                TypeCode.Double when IsWithinAllowedDoubleRange((IConvertible)value) =>
                    SettingType.Double,
                _ =>
                    SettingType.Unknown,
            };
        }

        public static Setting ToSetting(this object value)
        {
            var settingType = DetermineSettingType(value);
            if (settingType == SettingType.Unknown)
            {
                throw new ArgumentException($"Could not determine the setting type of {value ?? "(null)"}.");
            }

            return new Setting
            {
#if USE_NEWTONSOFT_JSON
                Value = new Newtonsoft.Json.Linq.JValue(value),
#else
                Value = Text.Json.JsonSerializer.SerializeToElement(value),
#endif
                SettingType = settingType
            };
        }

        public static TValue ConvertTo<TValue>(this JsonValue value)
        {
            Debug.Assert(typeof(TValue) != typeof(object), "Conversion to object is not supported.");

#if USE_NEWTONSOFT_JSON
            return Newtonsoft.Json.Linq.Extensions.Value<TValue>(value);
#else
            return System.Text.Json.JsonSerializer.Deserialize<TValue>(value);
#endif
        }
    }
}
