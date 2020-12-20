using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using Moq;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class SynchronizationContextDeadlockTests
    {
        private const string SDKKEY = "PKDVCLf-Hq-h-kCzMp-L7Q/psuH7BGHoUmdONrzzUOY7A";

        private readonly Mock<SynchronizationContext> syncContextMock;

        private static SynchronizationContext synchronizationContextBackup;

        public SynchronizationContextDeadlockTests()
        {
            synchronizationContextBackup = SynchronizationContext.Current;

            syncContextMock = new Mock<SynchronizationContext>
            {
                CallBase = true
            };

            SynchronizationContext.SetSynchronizationContext(syncContextMock.Object);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContextBackup);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            syncContextMock.Reset();
        }

        [TestMethod]
        public void AutoPollDeadLockCheck()
        {
            var client = new ConfigCatClient(new AutoPollConfiguration
            {
                SdkKey = SDKKEY,
                Logger = new ConsoleLogger(LogLevel.Off)                
            });

            ClientDeadlockCheck(client);
        }

        [TestMethod]
        public void ManualPollDeadLockCheck()
        {
            var client = new ConfigCatClient(new ManualPollConfiguration
            {
                SdkKey = SDKKEY,
                Logger = new ConsoleLogger(LogLevel.Off)
            });

            ClientDeadlockCheck(client);
        }

        [TestMethod]
        public void LazyLoadDeadLockCheck()
        {
            var client = new ConfigCatClient(new LazyLoadConfiguration
            {
                SdkKey = SDKKEY,
                Logger = new ConsoleLogger(LogLevel.Off)
            });

            ClientDeadlockCheck(client);
        }

        private void ClientDeadlockCheck(IConfigCatClient client)
        {
            var methods = typeof(IConfigCatClient).GetMethods().Where(x => !x.IsSpecialName).OrderBy(o => o.Name);

            foreach (var m in methods)
            {
                var parameters = Enumerable.Repeat<object>(null, m.GetParameters().Length).ToArray();

                MethodInfo mi = m;

                if (m.IsGenericMethod)
                {
                    mi = m.MakeGenericMethod(typeof(string));
                }

                Console.WriteLine($"Invoke '{mi.Name}' method");

                if (mi.ReturnType.IsSubclassOf(typeof(Task)))
                {
                    var task = (Task)mi.Invoke(client, parameters);

                    task.ConfigureAwait(false);
                    task.Wait();
                }
                else
                {
                    mi.Invoke(client, parameters);
                }

                syncContextMock.Verify(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Never, $"Method: {mi.Name}");
                syncContextMock.Verify(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Never, $"Method: {mi.Name}");
            }
        }
    }
}
