using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
[DoNotParallelize]
public class SynchronizationContextDeadlockTests
{
    private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

    private static SynchronizationContext? SynchronizationContextBackup;
    private readonly Mock<SynchronizationContext> syncContextMock;

    public SynchronizationContextDeadlockTests()
    {
        SynchronizationContextBackup = SynchronizationContext.Current;

        this.syncContextMock = new Mock<SynchronizationContext>
        {
            CallBase = true
        };

        SynchronizationContext.SetSynchronizationContext(this.syncContextMock.Object);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        SynchronizationContext.SetSynchronizationContext(SynchronizationContextBackup);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        this.syncContextMock.Reset();
    }

    [TestMethod]
    public void AutoPollDeadLockCheck()
    {
        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Off);
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        });

        ClientDeadlockCheck(client);
    }

    [TestMethod]
    public void ManualPollDeadLockCheck()
    {
        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Off);
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        });

        ClientDeadlockCheck(client);
    }

    [TestMethod]
    public void LazyLoadDeadLockCheck()
    {
        using var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Off);
            options.ConfigFetcher = ConfigFetcherHelper.CreateFetcherWithSharedHandler();
        });

        ClientDeadlockCheck(client);
    }

    private static readonly Dictionary<string, object?[]> SpecificMethodParams = new()
    {
        [nameof(ConfigCatClient.GetValue)] = new object?[] { "x", null, null },
        [nameof(ConfigCatClient.GetValueAsync)] = new object?[] { "x", null, null, CancellationToken.None },
        [nameof(ConfigCatClient.GetValueDetails)] = new object?[] { "x", null, null },
        [nameof(ConfigCatClient.GetValueDetailsAsync)] = new object?[] { "x", null, null, CancellationToken.None },
        [nameof(ConfigCatClient.GetKeyAndValue)] = new object?[] { "x" },
        [nameof(ConfigCatClient.GetKeyAndValueAsync)] = new object?[] { "x", CancellationToken.None },
        [nameof(ConfigCatClient.SetDefaultUser)] = new object?[] { new User("id") },
    };

    private void ClientDeadlockCheck(IConfigCatClient client)
    {
        var methods = typeof(IConfigCatClient).GetMethods()
            .Where(x => !x.IsSpecialName)
            .OrderBy(o => o.Name);

        foreach (var m in methods)
        {
            if (!SpecificMethodParams.TryGetValue(m.Name, out var parameters))
            {
                parameters = Enumerable.Repeat<object?>(null, m.GetParameters().Length).ToArray();
            }

            MethodInfo mi = m;

            if (m.IsGenericMethod)
            {
                mi = m.MakeGenericMethod(typeof(string));
            }

            Console.WriteLine($"Invoke '{mi.Name}' method");

            if (mi.ReturnType.IsSubclassOf(typeof(Task)))
            {
                var task = (Task)mi.Invoke(client, parameters)!;

                task.ConfigureAwait(false);
                task.Wait();
            }
            else
            {
                mi.Invoke(client, parameters);
            }

            this.syncContextMock.Verify(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Never, $"Method: {mi.Name}");
            this.syncContextMock.Verify(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Never, $"Method: {mi.Name}");
        }
    }
}
