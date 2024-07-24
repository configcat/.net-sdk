using System.Diagnostics;
using System.Globalization;
using ConfigCat.Client;
using ConfigCat.Client.Utils;

#if USE_NEWTONSOFT_JSON
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace System;

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

    internal static SettingValue ToSettingValue(this JsonValue value, out SettingType settingType)
    {
#if USE_NEWTONSOFT_JSON
        switch (value.Type)
        {
            case Newtonsoft.Json.Linq.JTokenType.String:
                settingType = SettingType.String;
                return new SettingValue { StringValue = value.ConvertTo<string>() };

            case Newtonsoft.Json.Linq.JTokenType.Boolean:
                settingType = SettingType.Boolean;
                return new SettingValue { BoolValue = value.ConvertTo<bool>() };

            case Newtonsoft.Json.Linq.JTokenType.Integer when IsWithinAllowedIntRange(value):
                settingType = SettingType.Int;
                return new SettingValue { IntValue = value.ConvertTo<int>() };

            case Newtonsoft.Json.Linq.JTokenType.Float when IsWithinAllowedDoubleRange(value):
                settingType = SettingType.Double;
                return new SettingValue { DoubleValue = value.ConvertTo<double>() };
        }
#else
        switch (value.ValueKind)
        {
            case Text.Json.JsonValueKind.String:
                settingType = SettingType.String;
                return new SettingValue { StringValue = value.ConvertTo(SourceGenSerializationContext.Default.String) };

            case Text.Json.JsonValueKind.False or Text.Json.JsonValueKind.True:
                settingType = SettingType.Boolean;
                return new SettingValue { BoolValue = value.ConvertTo(SourceGenSerializationContext.Default.Boolean) };

            case Text.Json.JsonValueKind.Number when value.TryGetInt32(out var _):
                settingType = SettingType.Int;
                return new SettingValue { IntValue = value.ConvertTo(SourceGenSerializationContext.Default.Int32) };

            case Text.Json.JsonValueKind.Number when value.TryGetDouble(out var _):
                settingType = SettingType.Double;
                return new SettingValue { DoubleValue = value.ConvertTo(SourceGenSerializationContext.Default.Double) };
        }
#endif

        settingType = Setting.UnknownType;
        return new SettingValue { UnsupportedValue = value };
    }

    public static SettingValue ToSettingValue(this object? value, out SettingType settingType)
    {
        if (value is not null)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.String:
                    settingType = SettingType.String;
                    return new SettingValue { StringValue = (string)value };

                case TypeCode.Boolean:
                    settingType = SettingType.Boolean;
                    return new SettingValue { BoolValue = (bool)value };

                case TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32:
                case TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 when IsWithinAllowedIntRange((IConvertible)value):
                    settingType = SettingType.Int;
                    return new SettingValue { IntValue = ((IConvertible)value).ToInt32(CultureInfo.InvariantCulture) };

                case TypeCode.Single or TypeCode.Double when IsWithinAllowedDoubleRange((IConvertible)value):
                    settingType = SettingType.Double;
                    return new SettingValue { DoubleValue = ((IConvertible)value).ToDouble(CultureInfo.InvariantCulture) };
            }
        }

        settingType = Setting.UnknownType;
        return new SettingValue { UnsupportedValue = value };
    }

    public static Setting ToSetting(this object? value)
    {
        var setting = new Setting
        {
            Value = value is JsonValue jsonValue
                ? jsonValue.ToSettingValue(out var settingType)
                : value.ToSettingValue(out settingType),
        };

        if (settingType != Setting.UnknownType)
        {
            setting.SettingType = settingType;
        }

        return setting;
    }

#if USE_NEWTONSOFT_JSON
    private static TValue ConvertTo<TValue>(this JsonValue value)
    {
        Debug.Assert(typeof(TValue) != typeof(object), "Conversion to object is not supported.");
        Debug.Assert(value.Type != Newtonsoft.Json.Linq.JTokenType.Null, "Tried to convert unexpected null value.");
        return Newtonsoft.Json.Linq.Extensions.Value<TValue>(value)!;
    }
#else
    private static TValue ConvertTo<TValue>(this JsonValue value, Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo)
    {
        Debug.Assert(typeof(TValue) != typeof(object), "Conversion to object is not supported.");
        Debug.Assert(value.ValueKind != Text.Json.JsonValueKind.Null, "Tried to convert unexpected null value.");
        return Text.Json.JsonSerializer.Deserialize(value, jsonTypeInfo)!;
    }
#endif

    public static bool TryConvertNumericToDouble(this object value, out double number)
    {
        if (Type.GetTypeCode(value.GetType()) is
           TypeCode.SByte or
           TypeCode.Byte or
           TypeCode.Int16 or
           TypeCode.UInt16 or
           TypeCode.Int32 or
           TypeCode.UInt32 or
           TypeCode.Int64 or
           TypeCode.UInt64 or
           TypeCode.Single or
           TypeCode.Double or
           TypeCode.Decimal)
        {
            number = ((IConvertible)value).ToDouble(CultureInfo.InvariantCulture);
            return true;
        }

        number = default;
        return false;
    }

    public static bool TryConvertDateTimeToDateTimeOffset(this object value, out DateTimeOffset dateTimeOffset)
    {
        if (value is DateTimeOffset dateTimeOffsetLocal)
        {
            dateTimeOffset = dateTimeOffsetLocal;
            return true;
        }
        else if (value is DateTime dateTime)
        {
            dateTimeOffset = new DateTimeOffset(dateTime.Kind != DateTimeKind.Unspecified ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
            return true;
        }

        dateTimeOffset = default;
        return false;
    }

    private static readonly object BoxedTrue = true;
    private static readonly object BoxedFalse = false;

    public static object AsCachedObject(this bool value) => value ? BoxedTrue : BoxedFalse;

    // In generic methods, we can't cast from/to the generic type directly even if we know that the conversion would be ok, that is,
    // something like (TValue)BoolValue won't work, we'd need (TValue)(object)BoolValue, which would mean boxing (memory allocation).
    // However, using the following trick involving delegates we can avoid boxing (see also https://stackoverflow.com/a/45508419).

    public static readonly Delegate BoxedIntToLong = new Func<object, long>(value => (int)value);
    public static readonly Delegate BoxedIntToNullableLong = new Func<object, long?>(value => (int)value);

    public static TTo Cast<TFrom, TTo>(this TFrom from, Delegate conversion) => ((Func<TFrom, TTo>)conversion)(from);
}
