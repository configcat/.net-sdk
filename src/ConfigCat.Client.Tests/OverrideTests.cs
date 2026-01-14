using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Override;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

using JsonValue = JsonElement;

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

        var snapshot = client.Snapshot();

        Assert.IsTrue(snapshot.GetValue("enabledFeature", false));
        Assert.IsFalse(snapshot.GetValue("disabledFeature", false));
        Assert.AreEqual(5, snapshot.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, snapshot.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", snapshot.GetValue("stringSetting", string.Empty));
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

    [TestMethod]
    public void LocalFile_Parallel()
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

        var snapshot = client.Snapshot();

        Parallel.ForEach(keys, item =>
        {
            Assert.IsNotNull(snapshot.GetValue<object?>(item, null));
        });
    }

    [TestMethod]
    public async Task LocalFileAsync_Parallel()
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

        var tasks = keys.Select(async item =>
        {
            Assert.IsNotNull(await client.GetValueAsync<object?>(item, null));
        });

        await Task.WhenAll(tasks);
    }

    [TestMethod]
    public async Task LocalFile_Default_WhenErrorOccures()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile("something-not-existing", false, OverrideBehaviour.LocalOnly);
        });

        Assert.IsFalse(await client.GetValueAsync("enabledFeature", false));
        Assert.AreEqual("default", await client.GetValueAsync("stringSetting", "default"));
    }

    [TestMethod]
    public void LocalFile_Read()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(ComplexJsonPath, true, OverrideBehaviour.LocalOnly);
        });

        var snapshot = client.Snapshot();

        Assert.IsTrue(snapshot.GetValue("enabledFeature", false));
        Assert.IsFalse(snapshot.GetValue("disabledFeature", false));
        Assert.AreEqual(5, snapshot.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, snapshot.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", snapshot.GetValue("stringSetting", string.Empty));
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
    public async Task LocalFile_Default_WhenErrorOccures_Reload()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile("something-not-existing", true, OverrideBehaviour.LocalOnly);
        });

        Assert.IsFalse(await client.GetValueAsync("enabledFeature", false));
        Assert.AreEqual("default", await client.GetValueAsync("stringSetting", "default"));
    }

    [TestMethod]
    public void LocalFile_Simple()
    {
        using var client = ConfigCatClient.Get("localhost", options =>
        {
            options.FlagOverrides = FlagOverrides.LocalFile(SimpleJsonPath, false, OverrideBehaviour.LocalOnly);
        });

        var snapshot = client.Snapshot();

        Assert.IsTrue(snapshot.GetValue("enabledFeature", false));
        Assert.IsFalse(snapshot.GetValue("disabledFeature", false));
        Assert.AreEqual(5, snapshot.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, snapshot.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", snapshot.GetValue("stringSetting", string.Empty));
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

        var snapshot = client.Snapshot();

        Assert.IsTrue(snapshot.GetValue("enabledFeature", false));
        Assert.IsFalse(snapshot.GetValue("disabledFeature", false));
        Assert.AreEqual(5, snapshot.GetValue("intSetting", 0));
        Assert.AreEqual(3.14, snapshot.GetValue("doubleSetting", 0.0));
        Assert.AreEqual("test", snapshot.GetValue("stringSetting", string.Empty));
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

        Assert.IsTrue(await client.GetValueAsync("fakeKey", false));
        Assert.IsTrue(await client.GetValueAsync("nonexisting", false));

        Assert.IsFalse(refreshResult.IsSuccess);
        Assert.AreEqual(RefreshErrorCode.LocalOnlyClient, refreshResult.ErrorCode);
        StringAssert.Contains(refreshResult.ErrorMessage, nameof(OverrideBehaviour.LocalOnly));
        Assert.IsNull(refreshResult.ErrorException);
    }

    [TestMethod]
    public async Task LocalOverRemote()
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

        var snapshot = client.Snapshot();

        Assert.IsTrue(snapshot.GetValue("fakeKey", false));
        Assert.IsTrue(snapshot.GetValue("nonexisting", false));
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
    public async Task RemoteOverLocal()
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

        var snapshot = client.Snapshot();

        Assert.IsFalse(snapshot.GetValue("fakeKey", false));
        Assert.IsTrue(snapshot.GetValue("nonexisting", false));
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

    [TestMethod]
    public async Task CustomDataSource()
    {
        // "Beta users" segment: Email IS ONE OF ['jane@example.com', 'john@example.com']
        const string configJson =
            """
            {
              "p": {
                "s": "UZWYnRWPwF7hApMquVrUmyPRGziigICYz372JOYqXgw=",
              },
              "s": [
                {
                  "n": "Beta users",
                  "r": [
                    {
                      "a": "Email",
                      "c": 16,
                      "l": [
                        "89f6d080752f2969b6802c399e6141885c4ce40fb151f41b9ec955c1f4790490",
                        "2dde8bd2436cb07d45fb455847f8a09ea2427313c278b3352a39db31e6106c4c",
                      ],
                    },
                  ],
                },
              ],
              "f": {
                "isInSegment": {
                  "t": 0,
                  "r": [
                    {
                      "c": [
                        {
                          "s": {
                            "s": 0,
                            "c": 0,
                          },
                        },
                      ],
                      "s": {
                        "v": {
                          "b": true,
                        },
                        "i": "1",
                      },
                    },
                  ],
                  "v": {
                    "b": false,
                  },
                  "i": "0",
                },
                "isNotInSegment": {
                  "t": 0,
                  "r": [
                    {
                      "c": [
                        {
                          "s": {
                            "s": 0,
                            "c": 1,
                          },
                        },
                      ],
                      "s": {
                        "v": {
                          "b": true,
                        },
                        "i": "1",
                      },
                    },
                  ],
                  "v": {
                    "b": false,
                  },
                  "i": "0",
                },
              },
            }
            """;

        var config = Config.Deserialize(configJson.AsSpan());

        var customDataSource = new ConfigBasedOverrideDataSource(config);

        using var client = ConfigCatClient.Get("localhost-123456789012/1234567890123456789012", options =>
        {
            options.FlagOverrides = new FlagOverrides(customDataSource, OverrideBehaviour.LocalOnly);
            options.PollingMode = PollingModes.ManualPoll;
        });


        var keys = client.Snapshot().GetAllKeys();
        CollectionAssert.AreEquivalent(new string[] { "isInSegment", "isNotInSegment" }, keys.ToArray());

        var user = new User("12345") { Email = "jane@example.com" };

        Assert.IsTrue(await client.GetValueAsync<bool?>("isInSegment", null, user));
        Assert.IsFalse(await client.GetValueAsync<bool?>("isNotInSegment", null, user));
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
    public async Task OverrideValueTypeMismatchShouldBeHandledCorrectly_Dictionary(object overrideValue, object defaultValue, object expectedEvaluatedValue)
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

        var method = typeof(IConfigCatClient).GetMethod(nameof(IConfigCatClient.GetValueDetailsAsync))!
            .GetGenericMethodDefinition()
            .MakeGenericMethod(defaultValue.GetType());

        var getValueDetailsValueTask = method.Invoke(client, new[] { key, defaultValue, null, CancellationToken.None })!;

        var asTaskMethod = typeof(ValueTask<>)
            .MakeGenericType(typeof(EvaluationDetails<>).MakeGenericType(defaultValue.GetType()))
            .GetMethod(nameof(ValueTask<object>.AsTask))!;

        var getValueDetailsTask = (Task)asTaskMethod.Invoke(getValueDetailsValueTask, Array.Empty<object>())!;

        await getValueDetailsTask;

        var actualEvaluatedValueDetails = (IEvaluationDetails)typeof(Task<>)
            .MakeGenericType(typeof(EvaluationDetails<>).MakeGenericType(defaultValue.GetType()))
            .GetProperty(nameof(Task<object>.Result))!
            .GetValue(getValueDetailsTask, null)!;
        var actualEvaluatedValue = actualEvaluatedValueDetails.Value;
        var actualEvaluatedValues = await client.GetAllValuesAsync(user: null);

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
    public async Task OverrideValueTypeMismatchShouldBeHandledCorrectly_SimplifiedConfig(string overrideValueJson, object defaultValue, object expectedEvaluatedValue)
    {
        const string key = "flag";
        var overrideValue = overrideValueJson.AsSpan().Deserialize<JsonValue>();

        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, $"{{ \"flags\": {{ \"{key}\": {overrideValueJson} }}, /* comment */ }}");

        try
        {
            using var client = ConfigCatClient.Get("localhost", options =>
            {
                options.PollingMode = PollingModes.ManualPoll;
                options.FlagOverrides = FlagOverrides.LocalFile(filePath, autoReload: false, OverrideBehaviour.LocalOnly);
            });

            var method = typeof(IConfigCatClient).GetMethod(nameof(IConfigCatClient.GetValueDetailsAsync))!
                .GetGenericMethodDefinition()
                .MakeGenericMethod(defaultValue.GetType());

            var getValueDetailsValueTask = method.Invoke(client, new[] { key, defaultValue, null, CancellationToken.None })!;

            var asTaskMethod = typeof(ValueTask<>)
                .MakeGenericType(typeof(EvaluationDetails<>).MakeGenericType(defaultValue.GetType()))
                .GetMethod(nameof(ValueTask<object>.AsTask))!;

            var getValueDetailsTask = (Task)asTaskMethod.Invoke(getValueDetailsValueTask, Array.Empty<object>())!;

            await getValueDetailsTask;

            var actualEvaluatedValueDetails = (IEvaluationDetails)typeof(Task<>)
                .MakeGenericType(typeof(EvaluationDetails<>).MakeGenericType(defaultValue.GetType()))
                .GetProperty(nameof(Task<object>.Result))!
                .GetValue(getValueDetailsTask, null)!;
            var actualEvaluatedValue = actualEvaluatedValueDetails.Value;
            var actualEvaluatedValues = await client.GetAllValuesAsync(user: null);

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

    private sealed class ConfigBasedOverrideDataSource : IOverrideDataSource
    {
        private readonly Config config;

        public ConfigBasedOverrideDataSource(Config config)
        {
            this.config = config;
        }

        public IReadOnlyDictionary<string, Setting> GetOverrides() => this.config.Settings;
    }
}
