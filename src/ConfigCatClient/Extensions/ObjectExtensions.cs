using System.Runtime.CompilerServices;
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

            SettingType DetermineSettingType(object value)
            {
#if !USE_NEWTONSOFT_JSON
                if (value is Text.Json.JsonElement element)
                {
                    if (element.ValueKind == Text.Json.JsonValueKind.Number)
                    {
                        if (element.TryGetInt32(out var _) || element.TryGetInt64(out var _)) return SettingType.Int;
                        if (element.TryGetDouble(out var _)) return SettingType.Double;
                    }

                    if (element.ValueKind == Text.Json.JsonValueKind.String) return SettingType.String;
                    if (element.ValueKind == Text.Json.JsonValueKind.True || element.ValueKind == Text.Json.JsonValueKind.False) return SettingType.Boolean;

                    throw new ArgumentException($"Could not determine the setting type of {value}");
                }
#endif

                var type = value.GetType();

                if (type == typeof(bool))
                    return SettingType.Boolean;

                if (type == typeof(int) || type == typeof(long))
                    return SettingType.Int;

                if (type == typeof(double))
                    return SettingType.Double;

                if (type == typeof(string))
                    return SettingType.String;

                throw new ArgumentException($"Could not determine the setting type of {value}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsAlive<T>(this WeakReference<T> weakRef) where T : class
        {
            return weakRef.TryGetTarget(out _);
        }
    }
}
