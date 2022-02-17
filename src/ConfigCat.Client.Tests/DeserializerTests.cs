using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class DeserializerTests
    {
        [TestMethod]
        public void Ensure_Global_Settings_Doesnt_Interfere()
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter { AllowIntegerValues = false });
                return settings;
            };

            var deserializer = new ConfigDeserializer();
            deserializer.TryDeserialize("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}", out var configs);
        }
    }
}
