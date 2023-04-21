using System;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConfigCat.Client.Tests;

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

        Assert.IsNotNull("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}".DeserializeOrDefault<SettingsWithPreferences>());
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public void ProjectConfig_Serialization_Works(bool isEmpty)
    {
        var pc = isEmpty
            ? ProjectConfig.Empty
            : ConfigHelper.FromString("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}", "\"ETAG\"", ProjectConfig.GenerateTimeStamp());

        var serializedPc = ProjectConfig.Serialize(pc);
        var deserializedPc = ProjectConfig.Deserialize(serializedPc);

        Assert.AreEqual(pc.ConfigJson, deserializedPc.ConfigJson);
        Assert.AreEqual(pc.HttpETag, deserializedPc.HttpETag);
        Assert.AreEqual(pc.TimeStamp, deserializedPc.TimeStamp);
        Assert.AreEqual(isEmpty, deserializedPc.IsEmpty);
    }
}
