using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.IO;

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

            var deserializer = new ConfigDeserializer(new LoggerWrapper(new ConsoleLogger(LogLevel.Debug)), JsonSerializer.Create());

            deserializer.TryDeserialize(new ProjectConfig("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}", DateTime.Now, ""), out var configs);
        }
    }
}
