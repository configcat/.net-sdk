using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

internal struct SettingValue
{
    private object? value;

    [JsonPropertyName("b")]
    public bool? BoolValue
    {
        readonly get => this.value as bool?;
        set => ModelHelper.SetOneOf(ref this.value, value?.AsCachedObject());
    }

    [JsonPropertyName("s")]
    public string? StringValue
    {
        readonly get => this.value as string;
        set => ModelHelper.SetOneOf(ref this.value, value);
    }

    [JsonPropertyName("i")]
    public int? IntValue
    {
        readonly get => this.value as int?;
        set => ModelHelper.SetOneOf(ref this.value, value);
    }

    [JsonPropertyName("d")]
    public double? DoubleValue
    {
        readonly get => this.value as double?;
        set => ModelHelper.SetOneOf(ref this.value, value);
    }

    [JsonIgnore]
    public object? UnsupportedValue
    {
        readonly get => (this.value as StrongBox<object?>)?.Value;
        set => this.value = new StrongBox<object?>(value);
    }

    [JsonIgnore]
    public readonly bool HasUnsupportedValue => this.value is StrongBox<object?>;

    public readonly object? GetValue(bool throwIfInvalid = true)
    {
        if (!ModelHelper.IsValidOneOf(this.value) || HasUnsupportedValue)
        {
            if (!throwIfInvalid)
            {
                return null;
            }

            // Value comes from a dictionary or simplified JSON flag override?
            if (HasUnsupportedValue)
            {
                var unsupportedValue = UnsupportedValue;
                throw new InvalidConfigModelException(unsupportedValue is not null
                    ? $"Setting value '{unsupportedValue}' is of an unsupported type ({unsupportedValue.GetType()})."
                    : "Setting value is null.");
            }
            // Value is missing or multiple values specified in the config JSON?
            else
            {
                throw new InvalidConfigModelException("Setting value is missing or invalid.");
            }
        }

        return this.value;
    }

    public readonly object? GetValue(SettingType settingType, bool throwIfInvalid = true)
    {
        var value = GetValue(throwIfInvalid);

        if (value is null || value.GetType().ToSettingType() != settingType)
        {
            return !throwIfInvalid ? null : throw new InvalidConfigModelException($"Setting value is not of the expected type {settingType}.");
        }

        return value;
    }

    public readonly TValue GetValue<TValue>(SettingType settingType)
    {
        var value = GetValue(settingType)!;

        // In the case of Int settings, we also allow long and long? return types.
        return typeof(TValue) switch
        {
            _ when typeof(TValue) == typeof(long) => value.Cast<object, TValue>(ObjectExtensions.BoxedIntToLong),
            _ when typeof(TValue) == typeof(long?) => value.Cast<object, TValue>(ObjectExtensions.BoxedIntToNullableLong),
            _ => (TValue)value,
        };
    }

    public override readonly string ToString()
    {
        return GetValue(throwIfInvalid: false) is { } value
            ? Convert.ToString(value, CultureInfo.InvariantCulture)!
            : EvaluateLogHelper.InvalidValuePlaceholder;
    }
}
