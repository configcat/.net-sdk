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
    }
}
