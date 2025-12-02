using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class ConfigCatClientSnapshotTests
{
    [TestMethod]
    public void DefaultInstanceDoesNotThrow()
    {
        const string key = "key";
        const string defaultValue = "";

        var snapshot = default(ConfigCatClientSnapshot);

        Assert.AreEqual(ClientCacheState.NoFlagData, snapshot.CacheState);
        Assert.IsNull(snapshot.FetchedConfig);
        CollectionAssert.AreEqual(Array.Empty<string>(), snapshot.GetAllKeys().ToArray());
        Assert.AreEqual("", snapshot.GetValue(key, defaultValue));
        var evaluationDetails = snapshot.GetValueDetails(key, defaultValue);
        Assert.IsNotNull(evaluationDetails);
        Assert.AreEqual(key, evaluationDetails.Key);
        Assert.AreEqual(defaultValue, evaluationDetails.Value);
        Assert.IsTrue(evaluationDetails.IsDefaultValue);
        Assert.IsNotNull(evaluationDetails.ErrorMessage);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CanMockSnapshot(bool success)
    {
        const ClientCacheState cacheState = ClientCacheState.HasUpToDateFlagData;
        var fetchedConfig = new Config();
        var keys = new[] { "key1", "key2" };
        var defaultValue = "";
        var user = new User("0");
        var evaluationDetails = success
            ? EvaluationDetails.Success(keys[0], "value", "var1", new TargetingRule(), new PercentageOption(), user, DateTime.UtcNow)
            : EvaluationDetails.Failure(keys[1], defaultValue, EvaluationErrorCode.UnexpectedError, "Something went wrong.", new Exception(), user, DateTime.UtcNow);

        var mock = new Mock<IConfigCatClientSnapshot>();
        mock.SetupGet(m => m.CacheState).Returns(cacheState);
        mock.SetupGet(m => m.FetchedConfig).Returns(fetchedConfig);
        mock.Setup(m => m.GetAllKeys()).Returns(keys);
        mock.Setup(m => m.GetValue(evaluationDetails.Key, It.IsAny<string>(), It.IsAny<User?>())).Returns(evaluationDetails.Value);
        mock.Setup(m => m.GetValueDetails(evaluationDetails.Key, It.IsAny<string>(), It.IsAny<User?>())).Returns(evaluationDetails);

        var snapshot = new ConfigCatClientSnapshot(mock.Object);

        Assert.AreEqual(cacheState, snapshot.CacheState);
        Assert.AreEqual(fetchedConfig, snapshot.FetchedConfig);
        CollectionAssert.AreEqual(keys, snapshot.GetAllKeys().ToArray());
        Assert.AreEqual(evaluationDetails.Value, snapshot.GetValue(evaluationDetails.Key, defaultValue, user));
        Assert.AreEqual(evaluationDetails, snapshot.GetValueDetails(evaluationDetails.Key, defaultValue, user));
    }
}
