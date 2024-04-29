using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if USE_NEWTONSOFT_JSON
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client.Tests;

[TestClass]
[DoNotParallelize]
public class OverrideTests
{
    private static readonly string ComplexJsonPath = Path.Combine("data", "test_json_complex.json");
    private static readonly string SimpleJsonPath = Path.Combine("data", "test_json_simple.json");
    private static readonly string SampleFileToCreate = Path.Combine("data", "generated.json");

    [TestMethod]
    public void LocalFile()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(ComplexJsonPath, false, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(client.GetValue("enabledFeature", false));
        Assert.IsFalse(client.GetValue("disabledFeature", false));
        Assert.AreEqual(5, client.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, client.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", client.GetValue("stringSetting", string.Empty));
    }

    [TestMethod]
    public async Task LocalFileAsync()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(ComplexJsonPath, false, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(await client.GetValueAsync("enabledFeature", false));
        Assert.IsFalse(await client.GetValueAsync("disabledFeature", false));
        Assert.AreEqual(5, await client.GetValueAsync("intSetting", 0));
        Assert.AreEqual(3.14, await client.GetValueAsync("doubleSetting", 0.0));
        Assert.AreEqual("test", await client.GetValueAsync("stringSetting", string.Empty));
    }

    public static void LocalFile_Parallel()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(ComplexJsonPath, false, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(client.GetValue("enabledFeature", false));
        Assert.IsFalse(client.GetValue("disabledFeature", false));
        Assert.AreEqual(5, client.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, client.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", client.GetValue("stringSetting", string.Empty));
    }

    [TestMethod]
    public void LocalFileAsync_Parallel()
    {
        var keys = new[]
        {
            "enabledFeature",
            "disabledFeature",
            "intSetting",
            "doubleSetting",
            "stringSetting",
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(ComplexJsonPath, false, OverrideBehaviour.LocalOnly);
        });

        Parallel.ForEach(keys, async item =>
        {
            Assert.IsNotNull(await client.GetValueAsync<object?>(item, null));
        });
    }

    [TestMethod]
    public void LocalFileAsync_Parallel_Sync()
    {
        var keys = new[]
        {
            "enabledFeature",
            "disabledFeature",
            "intSetting",
            "doubleSetting",
            "stringSetting",
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(ComplexJsonPath, false, OverrideBehaviour.LocalOnly);
        });

        Parallel.ForEach(keys, item =>
        {
            Assert.IsNotNull(client.GetValue<object?>(item, null));
        });
    }

    [TestMethod]
    public void LocalFile_Default_WhenErrorOccures()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile("something-not-existing", false, OverrideBehaviour.LocalOnly);
        });

        Assert.IsFalse(client.GetValue("enabledFeature", false));
        Assert.AreEqual("default", client.GetValue("stringSetting", "default"));
    }

    [TestMethod]
    public void LocalFile_Read()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(ComplexJsonPath, true, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(client.GetValue("enabledFeature", false));
        Assert.IsFalse(client.GetValue("disabledFeature", false));
        Assert.AreEqual(5, client.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, client.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", client.GetValue("stringSetting", string.Empty));
    }

    [TestMethod]
    public async Task LocalFileAsync_Read()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(ComplexJsonPath, true, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(await client.GetValueAsync("enabledFeature", false));
        Assert.IsFalse(await client.GetValueAsync("disabledFeature", false));
        Assert.AreEqual(5, await client.GetValueAsync("intSetting", 0));
        Assert.AreEqual(3.14, await client.GetValueAsync("doubleSetting", 0.0));
        Assert.AreEqual("test", await client.GetValueAsync("stringSetting", string.Empty));
    }

    [TestMethod]
    public void LocalFile_Default_WhenErrorOccures_Reload()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile("something-not-existing", true, OverrideBehaviour.LocalOnly);
        });

        Assert.IsFalse(client.GetValue("enabledFeature", false));
        Assert.AreEqual("default", client.GetValue("stringSetting", "default"));
    }

    [TestMethod]
    public void LocalFile_Simple()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(SimpleJsonPath, false, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(client.GetValue("enabledFeature", false));
        Assert.IsFalse(client.GetValue("disabledFeature", false));
        Assert.AreEqual(5, client.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, client.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", client.GetValue("stringSetting", string.Empty));
    }

    [TestMethod]
    public async Task LocalFileAsync_Simple()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(SimpleJsonPath, false, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(await client.GetValueAsync("enabledFeature", false));
        Assert.IsFalse(await client.GetValueAsync("disabledFeature", false));
        Assert.AreEqual(5, await client.GetValueAsync("intSetting", 0));
        Assert.AreEqual(3.14, await client.GetValueAsync("doubleSetting", 0.0));
        Assert.AreEqual("test", await client.GetValueAsync("stringSetting", string.Empty));
    }

    [TestMethod]
    public void LocalFile_Dictionary()
    {
        var dict = new Dictionary<string, object>
        {
            {"enabledFeature", true},
            {"disabledFeature", false},
            {"intSetting", 5},
            {"doubleSetting", 3.14},
            {"stringSetting", "test"},
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(client.GetValue("enabledFeature", false));
        Assert.IsFalse(client.GetValue("disabledFeature", false));
        Assert.AreEqual(5, client.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, client.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", client.GetValue("stringSetting", string.Empty));
    }

    [TestMethod]
    public async Task LocalFileAsync_Dictionary()
    {
        var dict = new Dictionary<string, object>
        {
            {"enabledFeature", true},
            {"disabledFeature", false},
            {"intSetting", 5},
            {"doubleSetting", 3.14},
            {"stringSetting", "test"},
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, OverrideBehaviour.LocalOnly);
        });

        Assert.IsTrue(await client.GetValueAsync("enabledFeature", false));
        Assert.IsFalse(await client.GetValueAsync("disabledFeature", false));
        Assert.AreEqual(5, await client.GetValueAsync("intSetting", 0));
        Assert.AreEqual(3.14, await client.GetValueAsync("doubleSetting", 0.0));
        Assert.AreEqual("test", await client.GetValueAsync("stringSetting", string.Empty));
    }

    [TestMethod]
    public void LocalOnly()
    {
        var dict = new Dictionary<string, object>
        {
            {"fakeKey", true},
            {"nonexisting", true},
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, OverrideBehaviour.LocalOnly);
            options.PollingMode = PollingModes.ManualPoll;
        });

        var refreshResult = client.ForceRefresh();

        Assert.IsTrue(client.GetValue("fakeKey", false));
        Assert.IsTrue(client.GetValue("nonexisting", false));

        Assert.IsFalse(refreshResult.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.LocalOnlyClient, refreshResult.ErrorCode);
        StringAssert.Contains(refreshResult.ErrorMessage, nameof(OverrideBehaviour.LocalOnly));
        Assert.IsNull(refreshResult.ErrorException);
    }

    [TestMethod]
    public void LocalOnly_Watch()
    {
        var dict = new Dictionary<string, object>
        {
            {"fakeKey", "test1"},
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, true, OverrideBehaviour.LocalOnly);
            options.PollingMode = PollingModes.ManualPoll;
        });

        Assert.AreEqual("test1", client.GetValue("fakeKey", string.Empty));

        dict["fakeKey"] = "test2";

        Assert.AreEqual("test2", client.GetValue("fakeKey", string.Empty));
    }

    [TestMethod]
    public async Task LocalOnly_Async_Watch()
    {
        var dict = new Dictionary<string, object>
        {
            {"fakeKey", "test1"},
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, true, OverrideBehaviour.LocalOnly);
            options.PollingMode = PollingModes.ManualPoll;
        });

        Assert.AreEqual("test1", await client.GetValueAsync("fakeKey", string.Empty));

        dict["fakeKey"] = "test2";

        Assert.AreEqual("test2", await client.GetValueAsync("fakeKey", string.Empty));
    }

    [TestMethod]
    public async Task LocalOnly_Async()
    {
        var dict = new Dictionary<string, object>
        {
            {"fakeKey", true},
            {"nonexisting", true},
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, OverrideBehaviour.LocalOnly);
            options.PollingMode = PollingModes.ManualPoll;
        });

        var refreshResult = await client.ForceRefreshAsync();

        Assert.IsTrue(client.GetValue("fakeKey", false));
        Assert.IsTrue(client.GetValue("nonexisting", false));

        Assert.IsFalse(refreshResult.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.LocalOnlyClient, refreshResult.ErrorCode);
        StringAssert.Contains(refreshResult.ErrorMessage, nameof(OverrideBehaviour.LocalOnly));
        Assert.IsNull(refreshResult.ErrorException);
    }

    [TestMethod]
    public void LocalOverRemote()
    {
        var dict = new Dictionary<string, object>
        {
            {"fakeKey", true},
            {"nonexisting", true},
        };

        var fakeHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK);

        using var client = ConfigCatClient.Get("localhost-123456789012/1234567890123456789012", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, OverrideBehaviour.LocalOverRemote);
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, GetJsonContent("false"));
            options.PollingMode = PollingModes.ManualPoll;
        });

        client.ForceRefresh();

        Assert.IsTrue(client.GetValue("fakeKey", false));
        Assert.IsTrue(client.GetValue("nonexisting", false));
    }

    [TestMethod]
    public async Task LocalOverRemote_Async()
    {
        var dict = new Dictionary<string, object>
        {
            {"fakeKey", true},
            {"nonexisting", true},
        };

        var fakeHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK);

        using var client = ConfigCatClient.Get("localhost-123456789012/1234567890123456789012", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, OverrideBehaviour.LocalOverRemote);
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, GetJsonContent("false"));
            options.PollingMode = PollingModes.ManualPoll;
        });

        await client.ForceRefreshAsync();

        Assert.IsTrue(await client.GetValueAsync("fakeKey", false));
        Assert.IsTrue(await client.GetValueAsync("nonexisting", false));
    }

    [TestMethod]
    public void RemoteOverLocal()
    {
        var dict = new Dictionary<string, object>
        {
            {"fakeKey", true},
            {"nonexisting", true},
        };

        var fakeHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK);

        using var client = ConfigCatClient.Get("localhost-123456789012/1234567890123456789012", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, OverrideBehaviour.RemoteOverLocal);
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, GetJsonContent("false"));
            options.PollingMode = PollingModes.ManualPoll;
        });

        client.ForceRefresh();

        Assert.IsFalse(client.GetValue("fakeKey", false));
        Assert.IsTrue(client.GetValue("nonexisting", false));
    }

    [TestMethod]
    public async Task RemoteOverLocal_Async()
    {
        var dict = new Dictionary<string, object>
        {
            {"fakeKey", true},
            {"nonexisting", true},
        };

        var fakeHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK);

        using var client = ConfigCatClient.Get("localhost-123456789012/1234567890123456789012", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalDictionary(dict, OverrideBehaviour.RemoteOverLocal);
            options.HttpClientHandler = new FakeHttpClientHandler(System.Net.HttpStatusCode.OK, GetJsonContent("false"));
            options.PollingMode = PollingModes.ManualPoll;
        });

        await client.ForceRefreshAsync();

        Assert.IsFalse(await client.GetValueAsync("fakeKey", false));
        Assert.IsTrue(await client.GetValueAsync("nonexisting", false));
    }

    [TestMethod]
    public async Task LocalFile_Watcher_Reload()
    {
        await CreateFileAndWriteContent(SampleFileToCreate, "initial");

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(SampleFileToCreate, true, OverrideBehaviour.LocalOnly);
        });
        client.LogLevel = LogLevel.Info;

        Assert.AreEqual("initial", await client.GetValueAsync("fakeKey", string.Empty));
        await Task.Delay(100);
        await WriteContent(SampleFileToCreate, "modified");
        await Task.Delay(1500);

        Assert.AreEqual("modified", await client.GetValueAsync("fakeKey", string.Empty));

        File.Delete(SampleFileToCreate);
    }

    [TestMethod]
    public async Task LocalFile_Watcher_Reload_Sync()
    {
        await CreateFileAndWriteContent(SampleFileToCreate, "initial");

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(SampleFileToCreate, true, OverrideBehaviour.LocalOnly);
        });
        client.LogLevel = LogLevel.Info;

        Assert.AreEqual("initial", client.GetValue("fakeKey", string.Empty));
        await Task.Delay(100);
        await WriteContent(SampleFileToCreate, "modified");
        await Task.Delay(1500);

        Assert.AreEqual("modified", client.GetValue("fakeKey", string.Empty));

        File.Delete(SampleFileToCreate);
    }

    [TestMethod]
    public async Task LocalFile_TolerantJsonParsing_SimplifiedConfig()
    {
        const string key = "flag";
        const bool expectedEvaluatedValue = true;
        var overrideValue = expectedEvaluatedValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();

        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, $"{{ \"flags\": {{ \"{key}\": {overrideValue} }}, /* comment */ }}");

        try
        {
            using var client = ConfigCatClient.Get("localhost", options =>
            {
                options.PollingMode = PollingModes.ManualPoll;
                options.FlagOverrides = FlagOverrides.LocalFile(filePath, autoReload: false, OverrideBehaviour.LocalOnly);
            });
            var actualEvaluatedValue = await client.GetValueAsync<bool?>(key, null);

            Assert.AreEqual(expectedEvaluatedValue, actualEvaluatedValue);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [TestMethod]
    public async Task LocalFile_TolerantJsonParsing_ComplexConfig()
    {
        const string key = "flag";
        const bool expectedEvaluatedValue = true;
        var overrideValue = expectedEvaluatedValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
        var settingType = ((int)SettingType.Boolean).ToString(CultureInfo.InvariantCulture);

        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, $"{{ \"f\": {{ \"{key}\": {{ \"t\": {settingType}, \"v\": {{ \"b\": {overrideValue} }} }} }}, /* comment */ }}");

        try
        {
            using var client = ConfigCatClient.Get("localhost", options =>
            {
                options.PollingMode = PollingModes.ManualPoll;
                options.FlagOverrides = FlagOverrides.LocalFile(filePath, autoReload: false, OverrideBehaviour.LocalOnly);
            });
            var actualEvaluatedValue = await client.GetValueAsync<bool?>(key, null);

            Assert.AreEqual(expectedEvaluatedValue, actualEvaluatedValue);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [DataRow(true, false, true)]
    [DataRow(true, "", "")]
    [DataRow(true, 0, 0)]
    [DataRow(true, 0.0, 0.0)]
    [DataRow("text", false, false)]
    [DataRow("text", "", "text")]
    [DataRow("text", 0, 0)]
    [DataRow("text", 0.0, 0.0)]
    [DataRow(42, false, false)]
    [DataRow(42, "", "")]
    [DataRow(42, 0, 42)]
    [DataRow(42, 0.0, 0.0)]
    [DataRow(42.0, false, false)]
    [DataRow(42.0, "", "")]
    [DataRow(42.0, 0, 0)]
    [DataRow(42.0, 0.0, 42.0)]
    [DataRow(3.14, false, false)]
    [DataRow(3.14, "", "")]
    [DataRow(3.14, 0, 0)]
    [DataRow(3.14, 0.0, 3.14)]
    [DataRow(null, false, false)]
    [DataRow(new object[0], false, false)]
    [DataRow(typeof(object), false, false)]
    [DataRow(typeof(DateTime), false, false)]
    [DataTestMethod]
    public void OverrideValueTypeMismatchShouldBeHandledCorrectly_Dictionary(object overrideValue, object defaultValue, object expectedEvaluatedValue)
    {
        const string key = "flag";

        var dictionary = new Dictionary<string, object>
        {
            [key] = overrideValue is not Type valueType ? overrideValue : Activator.CreateInstance(valueType)!
        };

        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.PollingMode = PollingModes.ManualPoll;
            options.FlagOverrides = FlagOverrides.LocalDictionary(dictionary, OverrideBehaviour.LocalOnly);
        });

        var method = typeof(IConfigCatClient).GetMethod(nameof(IConfigCatClient.GetValueDetails))!
            .GetGenericMethodDefinition()
            .MakeGenericMethod(defaultValue.GetType());

        var actualEvaluatedValueDetails = (EvaluationDetails)method.Invoke(client, new[] { key, defaultValue, null })!;
        var actualEvaluatedValue = actualEvaluatedValueDetails.Value;
        var actualEvaluatedValues = client.GetAllValues(user: null);

        Assert.AreEqual(expectedEvaluatedValue, actualEvaluatedValue);
        if (!defaultValue.Equals(expectedEvaluatedValue))
        {
            Assert.IsFalse(actualEvaluatedValueDetails.IsDefaultValue);
            Assert.AreEqual(EvaluationErrorCode.None, actualEvaluatedValueDetails.ErrorCode);
            Assert.IsNull(actualEvaluatedValueDetails.ErrorMessage);
            Assert.IsNull(actualEvaluatedValueDetails.ErrorException);
        }
        else
        {
            Assert.IsTrue(actualEvaluatedValueDetails.IsDefaultValue);
            Assert.IsNotNull(actualEvaluatedValueDetails.ErrorMessage);
            if (overrideValue.ToSettingValue(out _).HasUnsupportedValue)
            {
                Assert.AreEqual(EvaluationErrorCode.InvalidConfigModel, actualEvaluatedValueDetails.ErrorCode);
                Assert.IsInstanceOfType(actualEvaluatedValueDetails.ErrorException, typeof(InvalidConfigModelException));
            }
            else
            {
                Assert.AreEqual(EvaluationErrorCode.SettingValueTypeMismatch, actualEvaluatedValueDetails.ErrorCode);
                Assert.IsInstanceOfType(actualEvaluatedValueDetails.ErrorException, typeof(EvaluationErrorException));
            }
        }

        overrideValue.ToSettingValue(out var overrideValueSettingType);
        var expectedEvaluatedValues = new KeyValuePair<string, object?>[]
        {
             new(key, overrideValueSettingType != Setting.UnknownType ? overrideValue : null)
        };
        CollectionAssert.AreEquivalent(expectedEvaluatedValues, actualEvaluatedValues.ToArray());
    }

    [DataRow("true", false, true)]
    [DataRow("true", "", "")]
    [DataRow("true", 0, 0)]
    [DataRow("true", 0.0, 0.0)]
    [DataRow("\"text\"", false, false)]
    [DataRow("\"text\"", "", "text")]
    [DataRow("\"text\"", 0, 0)]
    [DataRow("\"text\"", 0.0, 0.0)]
    [DataRow("42", false, false)]
    [DataRow("42", "", "")]
    [DataRow("42", 0, 42)]
    [DataRow("42", 0.0, 0.0)]
    [DataRow("42.0", false, false)]
    [DataRow("42.0", "", "")]
    [DataRow("42.0", 0, 0)]
    [DataRow("42.0", 0.0, 42.0)]
    [DataRow("3.14", false, false)]
    [DataRow("3.14", "", "")]
    [DataRow("3.14", 0, 0)]
    [DataRow("3.14", 0.0, 3.14)]
    [DataRow("null", false, false)]
    [DataRow("[]", false, false)]
    [DataRow("{}", false, false)]
    [DataTestMethod]
    public void OverrideValueTypeMismatchShouldBeHandledCorrectly_SimplifiedConfig(string overrideValueJson, object defaultValue, object expectedEvaluatedValue)
    {
        const string key = "flag";
        var overrideValue =
#if USE_NEWTONSOFT_JSON
            overrideValueJson.AsMemory().Deserialize<Newtonsoft.Json.Linq.JToken>();
#else
            overrideValueJson.AsMemory().Deserialize<System.Text.Json.JsonElement>();
#endif

        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, $"{{ \"flags\": {{ \"{key}\": {overrideValueJson} }} }}");

        try
        {
            using var client = ConfigCatClient.Get("localhost", options =>
            {
                options.PollingMode = PollingModes.ManualPoll;
                options.FlagOverrides = FlagOverrides.LocalFile(filePath, autoReload: false, OverrideBehaviour.LocalOnly);
            });

            var method = typeof(IConfigCatClient).GetMethod(nameof(IConfigCatClient.GetValueDetails))!
                .GetGenericMethodDefinition()
                .MakeGenericMethod(defaultValue.GetType());

            var actualEvaluatedValueDetails = (EvaluationDetails)method.Invoke(client, new[] { key, defaultValue, null })!;
            var actualEvaluatedValue = actualEvaluatedValueDetails.Value;
            var actualEvaluatedValues = client.GetAllValues(user: null);

            Assert.AreEqual(expectedEvaluatedValue, actualEvaluatedValue);
            if (!defaultValue.Equals(expectedEvaluatedValue))
            {
                Assert.IsFalse(actualEvaluatedValueDetails.IsDefaultValue);
                Assert.AreEqual(EvaluationErrorCode.None, actualEvaluatedValueDetails.ErrorCode);
                Assert.IsNull(actualEvaluatedValueDetails.ErrorMessage);
                Assert.IsNull(actualEvaluatedValueDetails.ErrorException);
            }
            else
            {
                Assert.IsTrue(actualEvaluatedValueDetails.IsDefaultValue);
                Assert.IsNotNull(actualEvaluatedValueDetails.ErrorMessage);
#if USE_NEWTONSOFT_JSON
                if (overrideValue is not JsonValue overrideJsonValue || overrideJsonValue.ToSettingValue(out _).HasUnsupportedValue)
#else
                if (overrideValue.ToSettingValue(out _).HasUnsupportedValue)
#endif
                {
                    Assert.AreEqual(EvaluationErrorCode.InvalidConfigModel, actualEvaluatedValueDetails.ErrorCode);
                    Assert.IsInstanceOfType(actualEvaluatedValueDetails.ErrorException, typeof(InvalidConfigModelException));
                }
                else
                {
                    Assert.AreEqual(EvaluationErrorCode.SettingValueTypeMismatch, actualEvaluatedValueDetails.ErrorCode);
                    Assert.IsInstanceOfType(actualEvaluatedValueDetails.ErrorException, typeof(EvaluationErrorException));
                }
            }

            var unwrappedOverrideValue = overrideValue is JsonValue jsonValue
                ? jsonValue.ToSettingValue(out var overrideValueSettingType)
                : overrideValue.ToSettingValue(out overrideValueSettingType);

            var expectedEvaluatedValues = new KeyValuePair<string, object?>[]
            {
                new(key, overrideValueSettingType != Setting.UnknownType
                    ? unwrappedOverrideValue.GetValue(overrideValueSettingType)
                    : null)
            };

            CollectionAssert.AreEquivalent(expectedEvaluatedValues, actualEvaluatedValues.ToArray());
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private static string GetJsonContent(string value)
    {
        return "{\"f\":{\"fakeKey\":{\"t\":1,\"v\":{\"s\":\"" + value + "\"}}}}";
    }

    private static async Task CreateFileAndWriteContent(string path, string content)
    {
        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(GetJsonContent(content));
    }

    private static async Task WriteContent(string path, string content)
    {
        using var stream = File.OpenWrite(path);
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(GetJsonContent(content));
    }
}
