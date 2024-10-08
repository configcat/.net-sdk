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

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskShim"/> class.
    /// </summary>
    protected TaskShim() { }

    /// <inheritdoc cref="Task.Run(Func{Task}, CancellationToken)"/>
    public abstract Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)"/>
    public abstract Task Delay(TimeSpan delay, CancellationToken cancellationToken = default);
}
