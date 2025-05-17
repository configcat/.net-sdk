using System;
using System.Net.Http;
using System.Threading.Tasks;
using ConfigCat.Client.Shims;

namespace ConfigCat.Client.Configuration;

/// <summary>
/// Provides compatibility options to make the SDK work on platforms that are not fully standards compliant.
/// </summary>
#if NETSTANDARD
public sealed class PlatformCompatibilityOptions
#else
internal sealed class PlatformCompatibilityOptions
#endif
{
    internal bool continueOnCapturedContext;

    internal Func<HttpClientHandler?, IConfigCatConfigFetcher>? configFetcherFactory;

    internal TaskShim taskShim = TaskShim.Default;

#if NETSTANDARD
    private volatile bool frozen;

    internal void Freeze() => this.frozen = true;

    private void EnsureNotFrozen()
    {
        if (this.frozen)
        {
            throw new InvalidOperationException($"Platform compatibility options cannot be changed after the first instance of {nameof(ConfigCatClient)} has been created.");
        }
    }

    /// <summary>
    /// Configures the SDK to run in a Unity WebGL application. To make this actually work, it is necessary to provide an implementation for
    /// <see cref="IConfigCatConfigFetcher"/> and an implementation for <see cref="TaskShim"/> to workaround
    /// <see href="https://docs.unity3d.com/Manual/ScriptingRestrictions.html">the restrictions of the Unity WebGL environments</see>.
    /// </summary>
    /// <param name="taskShim">The implementation of a few <see cref="Task"/>-related APIs used by the SDK.</param>
    /// <param name="configFetcherFactory">The config fetcher implementation (based on e.g. <see href="https://docs.unity3d.com/2021.3/Documentation/ScriptReference/Networking.UnityWebRequest.html"/>.</param>
    public void EnableUnityWebGLCompatibility(TaskShim taskShim, Func<IConfigCatConfigFetcher> configFetcherFactory)
    {
        if (configFetcherFactory is null)
        {
            throw new ArgumentNullException(nameof(configFetcherFactory));
        }

        if (taskShim is null)
        {
            throw new ArgumentNullException(nameof(taskShim));
        }

        EnsureNotFrozen();

        this.continueOnCapturedContext = true;
        this.configFetcherFactory = (_) => configFetcherFactory();
        this.taskShim = taskShim;
    }
#endif
}
