using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Cache;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Override;
using ConfigCat.Client.Tests.Fakes;
using ConfigCat.Client.Tests.Helpers;
using ConfigCat.Client.Utils;
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
        CollectionAssert.AreEqual(ArrayUtils.EmptyArray<string>(), snapshot.GetAllKeys().ToArray());
        Assert.AreEqual("", snapshot.GetValue(key, defaultValue));
        var evaluationDetails = snapshot.GetValueDetails(key, defaultValue);
        Assert.IsNotNull(evaluationDetails);
        Assert.AreEqual(key, evaluationDetails.Key);
        Assert.AreEqual(defaultValue, evaluationDetails.Value);
        Assert.IsTrue(evaluationDetails.IsDefaultValue);
        Assert.IsNotNull(evaluationDetails.ErrorMessage);
    }

    [TestMethod]
    public void CanMockSnapshot()
    {
        const ClientCacheState cacheState = ClientCacheState.HasUpToDateFlagData;
        var fetchedConfig = new Config();
        var keys = new[] { "key1", "key2" };
        var evaluationDetails = new EvaluationDetails<string>("key1", "value");

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
        Assert.AreEqual(evaluationDetails.Value, snapshot.GetValue(evaluationDetails.Key, ""));
        Assert.AreSame(evaluationDetails, snapshot.GetValueDetails(evaluationDetails.Key, ""));
    }
}
