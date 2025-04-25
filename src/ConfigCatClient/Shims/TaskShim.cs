using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client.Shims;

/// <summary>
/// Defines an abstraction over a few <see cref="Task"/>-related APIs used by the SDK so that
/// consumers can override their behavior in some constrained runtime environments.
/// </summary>
#if NETSTANDARD
public abstract class TaskShim
#else
internal abstract class TaskShim
#endif
{
    /// <summary>
    /// Provides an instance of the default implementation of the <see cref="TaskShim"/> class, which just simply calls the built-in <see cref="Task"/> methods.
    /// </summary>
    public static readonly TaskShim Default = new DefaultTaskShim();

    /// <summary>
    /// Returns the currently used <see cref="TaskShim"/>, configured via <see cref="PlatformCompatibilityOptions"/>.
    /// </summary>
    public static TaskShim Current => ConfigCatClient.PlatformCompatibilityOptions.taskShim;

    internal static readonly Task<object?> CompletedTask = Task.FromResult<object?>(null);

    internal static bool ContinueOnCapturedContext => ConfigCatClient.PlatformCompatibilityOptions.continueOnCapturedContext;

    internal static TaskCompletionSource<TResult> CreateSafeCompletionSource<TResult>(out Task<TResult> task, CancellationToken cancellationToken = default)
    {
        // See also:
        // * https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        // * https://blog.stephencleary.com/2012/12/dont-block-in-asynchronous-code.html

#if !NET45
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        task = tcs.Task;
#else
        var tcs = new TaskCompletionSource<TResult>();
        task = tcs.Task
            .ContinueWith(task => task.GetAwaiter().GetResult(), cancellationToken, TaskContinuationOptions.LazyCancellation, TaskScheduler.Default);
#endif

        return tcs;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskShim"/> class.
    /// </summary>
    protected TaskShim() { }

    /// <inheritdoc cref="Task.Run(Func{Task}, CancellationToken)"/>
    public abstract Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)"/>
    public abstract Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
}
