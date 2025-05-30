using System;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class DeserializerTests
{
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
