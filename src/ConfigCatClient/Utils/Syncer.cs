using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Utils
{
    internal static class Syncer
    {
        private static readonly TaskFactory TaskFactory = new(CancellationToken.None,
                TaskCreationOptions.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

        internal static TResult Sync<TResult>(Func<Task<TResult>> func) => TaskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();

        internal static void Sync(Func<Task> func) => TaskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
    }
}
