using ConfigCat.Client.Shims;

namespace System.Threading.Tasks;

internal static class TaskExtensions
{
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
            return Task.FromCanceled<T>(cancellationToken);
        }

        return Awaited(task, cancellationToken);

        static async Task<T> Awaited(Task task, CancellationToken cancellationToken)
        {
            var cancellationTcs = TaskShim.CreateSafeCompletionSource<T>();
            var tokenRegistration = cancellationToken.Register(() => cancellationTcs.TrySetCanceled(cancellationToken), useSynchronizationContext: TaskShim.ContinueOnCapturedContext);

            using (tokenRegistration)
            {
                var completedTask = await Task.WhenAny(task, cancellationTcs.Task).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                if (completedTask is Task<T> taskWithResult)
                {
                    return taskWithResult.GetAwaiter().GetResult();
                }
                else
                {
                    // Although the task has no return value, the potential cancellation or exception still needs to be propagated.
                    completedTask.GetAwaiter().GetResult();
                    return default!;
                }
            }
        }
    }
#endif
}
