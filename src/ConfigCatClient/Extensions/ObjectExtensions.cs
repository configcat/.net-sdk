using ConfigCat.Client.Evaluate;

namespace System
{
    internal static class ObjectExtensions
    {
        public static Setting ToSetting(this object value)
        {
            return new Setting
            {
#if USE_NEWTONSOFT_JSON
                Value = new Newtonsoft.Json.Linq.JValue(value),
#else
                Value = Text.Json.JsonSerializer.SerializeToElement(value),
#endif
                SettingType = DetermineSettingType(value)
            };

            SettingTypeEnum DetermineSettingType(object value)
            {
#if !USE_NEWTONSOFT_JSON
                if (value is Text.Json.JsonElement element)
                {
                    if (element.ValueKind == Text.Json.JsonValueKind.Number)
                    {
                        if (element.TryGetInt32(out var _) || element.TryGetInt64(out var _)) return SettingTypeEnum.Int;
                        if (element.TryGetDouble(out var _)) return SettingTypeEnum.Double;
                    }

                    if (element.ValueKind == Text.Json.JsonValueKind.String) return SettingTypeEnum.String;
                    if (element.ValueKind == Text.Json.JsonValueKind.True || element.ValueKind == Text.Json.JsonValueKind.False) return SettingTypeEnum.Boolean;

                    throw new ArgumentException($"Could not determine the setting type of {value}");
                }
#endif

                var type = value.GetType();

                if (type == typeof(bool))
                    return SettingTypeEnum.Boolean;

                if (type == typeof(int) || type == typeof(long))
                    return SettingTypeEnum.Int;

                if (type == typeof(double))
                    return SettingTypeEnum.Double;

                if (type == typeof(string))
                    return SettingTypeEnum.String;

                throw new ArgumentException($"Could not determine the setting type of {value}");
            }
        }
    }
}
