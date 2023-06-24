using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

internal struct SettingValue
{
    private object? value;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "b")]
#else
    [JsonPropertyName("b")]
#endif
    public bool? BoolValue
    {
        readonly get => this.value as bool?;
        set => ModelHelper.SetOneOf(ref this.value, value?.AsCachedObject());
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "s")]
#else
    [JsonPropertyName("s")]
#endif
    public string? StringValue
    {
        readonly get => this.value as string;
        set => ModelHelper.SetOneOf(ref this.value, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "i")]
#else
    [JsonPropertyName("i")]
#endif
    public int? IntValue
    {
        readonly get => this.value as int?;
        set => ModelHelper.SetOneOf(ref this.value, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "d")]
#else
    [JsonPropertyName("d")]
#endif
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
                throw new InvalidOperationException(unsupportedValue is not null
                    ? $"Setting value '{unsupportedValue}' is of an unsupported type ({unsupportedValue.GetType()})."
                    : "Setting value is null.");
            }
            // Value is missing or multiple values specified in the config JSON?
            else
            {
                throw new InvalidOperationException("Setting value is missing or invalid.");
            }
        }

        return this.value;
    }

    public readonly object? GetValue(SettingType settingType, bool throwIfInvalid = true)
    {
        var value = GetValue(throwIfInvalid);

        if (value is null || value.GetType().ToSettingType() != settingType)
        {
            return !throwIfInvalid ? null : throw new InvalidOperationException($"Setting value is not of the expected type {settingType}.");
        }

        return value;
    }

    public readonly TValue GetValue<TValue>(SettingType settingType)
    {
        var value = GetValue(settingType)!;

        // In the case of Int settings, we also allow long and long? return types.
        return typeof(TValue) switch
        {
            var type when type == typeof(long) => value.Cast<object, TValue>(ObjectExtensions.BoxedIntToLong),
            var type when type == typeof(long?) => value.Cast<object, TValue>(ObjectExtensions.BoxedIntToNullableLong),
            _ => (TValue)value,
        };
    }

    public override readonly string ToString()
    {
        return GetValue(throwIfInvalid: false) is { } value
            ? Convert.ToString(value, CultureInfo.InvariantCulture)!
            : RolloutEvaluator.InvalidValuePlaceholder;
    }
}
