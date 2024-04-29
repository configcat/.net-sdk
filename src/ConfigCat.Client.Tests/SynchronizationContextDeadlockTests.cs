using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
[DoNotParallelize]
public class SynchronizationContextDeadlockTests
{
    private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

    private static readonly HttpClientHandler SharedHandler = new();

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
            options.HttpClientHandler = SharedHandler;
        });

        ClientDeadlockCheck(client);
    }

    [TestMethod]
    public void ManualPollDeadLockCheck()
    {
        var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Off);
            options.HttpClientHandler = SharedHandler;
        });

        ClientDeadlockCheck(client);
    }

    [TestMethod]
    public void LazyLoadDeadLockCheck()
    {
        var client = ConfigCatClient.Get(SDKKEY, options =>
        {
            options.Logger = new ConsoleLogger(LogLevel.Off);
            options.HttpClientHandler = SharedHandler;
        });

        ClientDeadlockCheck(client);
    }

    private static readonly Dictionary<string, object?[]> SpecificMethodParams = new()
    {
        ["GetValue"] = new object?[] { "x", null, null },
        ["GetValueAsync"] = new object?[] { "x", null, null, CancellationToken.None },
        ["GetValueDetails"] = new object?[] { "x", null, null },
        ["GetValueDetailsAsync"] = new object?[] { "x", null, null, CancellationToken.None },
        ["GetKeyAndValue"] = new object?[] { "x" },
        ["GetKeyAndValueAsync"] = new object?[] { "x", CancellationToken.None },
        ["SetDefaultUser"] = new object?[] { new User("id") },
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
