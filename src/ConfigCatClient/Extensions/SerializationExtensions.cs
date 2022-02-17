using System;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
using System.IO;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace System
{
    internal static class SerializationExtensions
    {

#if USE_NEWTONSOFT_JSON
        private static readonly JsonSerializer Serializer = JsonSerializer.Create();
#endif

        public static T Deserialize<T>(this string json)
        {
#if USE_NEWTONSOFT_JSON
            using var stringReader = new StringReader(json);
            using var reader = new JsonTextReader(stringReader);
            return Serializer.Deserialize<T>(reader);
#else
            return JsonSerializer.Deserialize<T>(json);
#endif
        }

        public static T DeserializeOrDefault<T>(this string json)
        {
            try
            {
                return json.Deserialize<T>();
            }
            catch
            {
                return default;
            }
        }

        public static string Serialize<T>(this T objectToSerialize)
        {
#if USE_NEWTONSOFT_JSON
            return JsonConvert.SerializeObject(objectToSerialize);
#else
            return JsonSerializer.Serialize(objectToSerialize);
#endif
        }

#if !USE_NEWTONSOFT_JSON
        private class ObjectDeserializer : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var type = reader.TokenType;

                if (type == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out var intValue)) return intValue;
                    if (reader.TryGetInt64(out var longValue)) return longValue;
                    if (reader.TryGetDouble(out var doubleValue)) return doubleValue;
                }

                if (type == JsonTokenType.String) return reader.GetString();

                if (type == JsonTokenType.True || type == JsonTokenType.False) return reader.GetBoolean();

                using var document = JsonDocument.ParseValue(ref reader);
                return document.RootElement.Clone();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
#endif
    }
}
