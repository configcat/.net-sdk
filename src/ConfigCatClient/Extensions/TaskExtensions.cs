namespace System.Threading.Tasks;

internal static class TaskExtensions
{
    // Shim for Task.FromCanceled(CancellationToken) missing from .NET 4.5

    public static Task ToTask(this CancellationToken cancellationToken)
    {
#if !NET45
        return Task.FromCanceled(cancellationToken);
#else
        return cancellationToken.ToTask<object>();
#endif
    }

    public static Task<T> ToTask<T>(this CancellationToken cancellationToken)
    {
#if !NET45
        return Task.FromCanceled<T>(cancellationToken);
#else
        return cancellationToken.IsCancellationRequested
            ? new Task<T>(static () => default!, cancellationToken)
            : throw new ArgumentOutOfRangeException(nameof(cancellationToken));
#endif
    }

#if !NET6_0_OR_GREATER
    // Polyfill for Task.WaitAsync(CancellationToken) introduced in .NET 6
    // See also: https://github.com/dotnet/runtime/blob/v6.0.13/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/Task.cs#L2758

    public static Task WaitAsync(this Task task, CancellationToken cancellationToken) => task.WaitCoreAsync<object>(cancellationToken);

    public static Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken) => (Task<T>)task.WaitCoreAsync<T>(cancellationToken);

    private static Task WaitCoreAsync<T>(this Task task, CancellationToken cancellationToken)
    {
        if (task.IsCompleted || !cancellationToken.CanBeCanceled)
        {
            return task;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return cancellationToken.ToTask<T>();
        }

        return Awaited(task, cancellationToken);

        static async Task<T> Awaited(Task task, CancellationToken cancellationToken)
        {
#if !NET45
            var cancellationTcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancellationTask = cancellationTcs.Task;
            var tokenRegistration = cancellationToken.Register(() => cancellationTcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
#else
            var cancellationTcs = new TaskCompletionSource<T>();
            var cancellationTask = cancellationTcs.Task.ContinueWith<T>(static _ => default!, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);
            var tokenRegistration = cancellationToken.Register(() => cancellationTcs.TrySetResult(default!), useSynchronizationContext: false);
#endif

            using (tokenRegistration)
            {
                var completedTask = await Task.WhenAny(task, cancellationTask).ConfigureAwait(false);
                return completedTask is Task<T> taskWithResult ? taskWithResult.GetAwaiter().GetResult() : default!;
            }
        }
    }
#endif
}
