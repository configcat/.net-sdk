using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class VariationIdTests
{
    private const string TestJson = "{ \"p\":{ \"u\": \"https://cdn-global.configcat.com\", \"r\": 0, \"s\": \"test-salt\"}, \"f\":{ \"key1\":{ \"t\":0, \"r\":[ { \"c\":[ { \"u\":{ \"a\": \"Email\", \"c\": 2 , \"l \":[ \"@configcat.com\" ] } } ], \"s\":{ \"v\": { \"b\":true }, \"i\": \"rolloutId1\" } }, { \"c\": [ { \"u\" :{ \"a\": \"Email\", \"c\": 2, \"l\" : [ \"@test.com\" ] } } ], \"s\" : { \"v\" : { \"b\": false }, \"i\": \"rolloutId2\" } } ], \"p\":[ { \"p\":50, \"v\" : { \"b\": true }, \"i\" : \"percentageId1\"  },  { \"p\" : 50, \"v\" : { \"b\": false }, \"i\": \"percentageId2\" } ], \"v\":{ \"b\":true }, \"i\": \"fakeId1\" }, \"key2\": { \"t\":0, \"v\": { \"b\": false }, \"i\": \"fakeId2\" }, \"key3\": { \"t\": 0, \"r\":[ { \"c\": [ { \"u\":{ \"a\": \"Email\", \"c\":2,  \"l\":[ \"@configcat.com\" ] } } ], \"p\": [{ \"p\":50, \"v\":{ \"b\": true  }, \"i\" : \"targetPercentageId1\" },  { \"p\": 50, \"v\": { \"b\":false }, \"i\" : \"targetPercentageId2\" } ] } ], \"v\":{ \"b\": false  }, \"i\": \"fakeId3\" } } }";
    private const string TestJsonIncorrect = "{ \"p\":{ \"u\": \"https://cdn-global.configcat.com\", \"r\": 0, \"s\": \"test-salt\" }, \"f\" :{ \"incorrect\" : { \"t\": 0, \"r\": [ {\"c\": [ {\"u\": {\"a\": \"Email\", \"c\": 2, \"l\": [\"@configcat.com\"] } } ] } ],\"v\": {\"b\": false}, \"i\": \"incorrectId\" } } }";

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetVariationId_Works(bool useSnapshot)
    {
        using var client = CreateClient(useSnapshot, TestJson);

        const string key = "key1";
        var valueDetails = useSnapshot
            ? client.Snapshot().GetValueDetails<bool?>(key, null)
            : await client.GetValueDetailsAsync<bool?>(key, null);
        Assert.AreEqual("fakeId1", valueDetails.VariationId);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetVariationId_NotFound(bool useSnapshot)
    {
        using var client = CreateClient(useSnapshot, TestJson);

        const string key = "nonexisting";
        var valueDetails = useSnapshot
            ? client.Snapshot().GetValueDetails<bool?>(key, null)
            : await client.GetValueDetailsAsync<bool?>(key, null);
        Assert.IsNull(valueDetails.VariationId);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetAllVariationIds_Works(bool useSnapshot)
    {
        using var client = CreateClient(useSnapshot, TestJson);

        ConfigCatClientSnapshot snapshot;
        EvaluationDetails[] allValueDetails = useSnapshot
            ? (snapshot = client.Snapshot()).GetAllKeys()
                .Select(keys => (EvaluationDetails)snapshot.GetValueDetails<object?>(keys, null))
                .ToArray()
            : (await client.GetAllValueDetailsAsync()).ToArray();

        Assert.AreEqual(3, allValueDetails.Length);

        Array.Sort(allValueDetails, (x, y) => StringComparer.Ordinal.Compare(x.Key, y.Key));
        Assert.AreEqual("fakeId1", allValueDetails[0].VariationId);
        Assert.AreEqual("fakeId2", allValueDetails[1].VariationId);
        Assert.AreEqual("fakeId3", allValueDetails[2].VariationId);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetAllVariationIds_Works_Empty(bool useSnapshot)
    {
        using var client = CreateClient(useSnapshot, "{}");

        ConfigCatClientSnapshot snapshot;
        EvaluationDetails[] allValueDetails = useSnapshot
            ? (snapshot = client.Snapshot()).GetAllKeys()
                .Select(keys => (EvaluationDetails)snapshot.GetValueDetails<object?>(keys, null))
                .ToArray()
            : (await client.GetAllValueDetailsAsync()).ToArray();

        Assert.AreEqual(0, allValueDetails.Length);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetKeyAndValue_Works(bool useSnapshot)
    {
        using var client = CreateClient(useSnapshot, TestJson);

        async Task<KeyValuePair<string, bool>?> GetKeyAndValue(string variationId)
        {
            return useSnapshot
                ? client.Snapshot().GetKeyAndValue<bool>(variationId)
                : await client.GetKeyAndValueAsync<bool>(variationId);
        }

        var result = await GetKeyAndValue("fakeId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key2", result.Value.Key);
        Assert.IsFalse(result.Value.Value);

        result = await GetKeyAndValue("percentageId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key1", result.Value.Key);
        Assert.IsFalse(result.Value.Value);

        result = await GetKeyAndValue("rolloutId1");
        Assert.IsNotNull(result);
        Assert.AreEqual("key1", result.Value.Key);
        Assert.IsTrue(result.Value.Value);

        result = await GetKeyAndValue("targetPercentageId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key3", result.Value.Key);
        Assert.IsFalse(result.Value.Value);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetKeyAndValue_Works_Nullable(bool useSnapshot)
    {
        using var client = CreateClient(useSnapshot, TestJson);

        async Task<KeyValuePair<string, bool?>?> GetKeyAndValue(string variationId)
        {
            return useSnapshot
                ? client.Snapshot().GetKeyAndValue<bool?>(variationId)
                : await client.GetKeyAndValueAsync<bool?>(variationId);
        }

        var result = await GetKeyAndValue("fakeId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key2", result.Value.Key);
        Assert.IsFalse(result.Value.Value);

        result = await GetKeyAndValue("percentageId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key1", result.Value.Key);
        Assert.IsFalse(result.Value.Value);

        result = await GetKeyAndValue("rolloutId1");
        Assert.IsNotNull(result);
        Assert.AreEqual("key1", result.Value.Key);
        Assert.IsTrue(result.Value.Value);

        result = await GetKeyAndValue("targetPercentageId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key3", result.Value.Key);
        Assert.IsFalse(result.Value.Value);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetKeyAndValue_Works_Object(bool useSnapshot)
    {
        using var client = CreateClient(useSnapshot, TestJson);

        async Task<KeyValuePair<string, object>?> GetKeyAndValue(string variationId)
        {
            return useSnapshot
                ? client.Snapshot().GetKeyAndValue<object>(variationId)
                : await client.GetKeyAndValueAsync<object>(variationId);
        }

        var result = await GetKeyAndValue("fakeId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key2", result.Value.Key);
        Assert.AreEqual(false, result.Value.Value);

        result = await GetKeyAndValue("percentageId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key1", result.Value.Key);
        Assert.AreEqual(false, result.Value.Value);

        result = await GetKeyAndValue("rolloutId1");
        Assert.IsNotNull(result);
        Assert.AreEqual("key1", result.Value.Key);
        Assert.AreEqual(true, result.Value.Value);

        result = await GetKeyAndValue("targetPercentageId2");
        Assert.IsNotNull(result);
        Assert.AreEqual("key3", result.Value.Key);
        Assert.AreEqual(false, result.Value.Value);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetKeyAndValue_NotFound(bool useSnapshot)
    {
        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents);

        using var client = CreateClient(useSnapshot, TestJson, logger);

        const string variationId = "nonexisting";
        var result = useSnapshot
            ? client.Snapshot().GetKeyAndValue<bool>(variationId)
            : await client.GetKeyAndValueAsync<bool>(variationId);

        Assert.IsNull(result);

        Assert.AreEqual(1, logEvents.Count);
        Assert.AreEqual(2011, logEvents[0].EventId);
        Assert.IsNull(logEvents[0].Exception);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetKeyAndValue_TypeMismatch(bool useSnapshot)
    {
        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents);

        using var client = CreateClient(useSnapshot, TestJson, logger);

        const string variationId = "fakeId2";
        var result = useSnapshot
            ? client.Snapshot().GetKeyAndValue<string>(variationId)
            : await client.GetKeyAndValueAsync<string>(variationId);

        Assert.IsNull(result);

        Assert.AreEqual(1, logEvents.Count);
        Assert.AreEqual(1002, logEvents[0].EventId);
        Assert.IsNotNull(logEvents[0].Exception);
        StringAssert.Contains(logEvents[0].Exception!.Message, "is not of the expected type");
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task GetKeyAndValue_IncorrectTargetingRule(bool useSnapshot)
    {
        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents);

        using var client = CreateClient(useSnapshot, TestJsonIncorrect, logger);

        const string variationId = "targetPercentageId2";
        var result = useSnapshot
            ? client.Snapshot().GetKeyAndValue<bool>(variationId)
            : await client.GetKeyAndValueAsync<bool>(variationId);

        Assert.IsNull(result);

        Assert.AreEqual(1, logEvents.Count);
        Assert.AreEqual(1002, logEvents[0].EventId);
        Assert.IsNotNull(logEvents[0].Exception);
        StringAssert.Contains(logEvents[0].Exception!.Message, "THEN part is missing or invalid");
    }

    private static ConfigCatClient CreateClient(bool useSnapshot, string configJson, IConfigCatLogger? logger = null)
    {
        logger ??= new Mock<IConfigCatLogger>().Object;

        var evaluator = new RolloutEvaluator(logger.AsWrapper());
        var configServiceMock = new Mock<IConfigService>();

        var config = ConfigHelper.FromString(configJson, "\"123\"", DateTime.UtcNow);
        if (useSnapshot)
        {
            configServiceMock.Setup(m => m.GetInMemoryConfig()).Returns(config);
        }
        else
        {
            configServiceMock.Setup(m => m.GetConfigAsync(It.IsAny<CancellationToken>())).ReturnsAsync(config);
        }

        return new ConfigCatClient(configServiceMock.Object, logger, evaluator);
    }
}
