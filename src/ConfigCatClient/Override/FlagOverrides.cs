using System;
using System.Collections.Generic;
using ConfigCat.Client.Override;

namespace ConfigCat.Client;

/// <summary>
/// Represents a flag override along with its settings. Also provides static factory methods for defining flag overrides.
/// </summary>
public class FlagOverrides
{
    private readonly string? filePath;
    private readonly bool autoReload;

    private readonly IDictionary<string, object>? dictionary;

    private FlagOverrides(string filePath, bool autoReload, OverrideBehaviour overrideBehaviour)
    {
        this.filePath = filePath;
        this.autoReload = autoReload;
        OverrideBehaviour = overrideBehaviour;
    }

    private FlagOverrides(IDictionary<string, object> dictionary, bool watchChanges, OverrideBehaviour overrideBehaviour)
    {
        this.dictionary = dictionary;
        this.autoReload = watchChanges;
        OverrideBehaviour = overrideBehaviour;
    }

    /// <summary>
    /// The override behaviour.
    /// </summary>
    public OverrideBehaviour OverrideBehaviour { get; }

    internal IOverrideDataSource BuildDataSource(LoggerWrapper logger)
    {
        if (this.dictionary is not null)
            return new LocalDictionaryDataSource(this.dictionary, this.autoReload);

        if (this.filePath is not null)
            return new LocalFileDataSource(this.filePath, this.autoReload, logger);

        throw new InvalidOperationException("Could not determine the right override data source.");
    }

    /// <summary>
    /// Creates an instance of the <see cref="FlagOverrides"/> class that uses a local file data source.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="autoReload">If set to <see langword="true"/>, the file will be reloaded when it gets modified.</param>
    /// <param name="overrideBehaviour">The override behaviour. Specifies whether the local values should override the remote values
    /// or local values should only be used when a remote value doesn't exist or the local values should be used only.</param>
    /// <returns>The new <see cref="FlagOverrides"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <see langword="null"/>.</exception>
    public static FlagOverrides LocalFile(string filePath, bool autoReload, OverrideBehaviour overrideBehaviour) =>
        new(filePath ?? throw new ArgumentNullException(nameof(filePath)), autoReload, overrideBehaviour);

    /// <summary>
    /// Creates an instance of the <see cref="FlagOverrides"/> class that uses a dictionary data source.
    /// </summary>
    /// <param name="dictionary">The dictionary that contains the overrides.</param>
    /// <param name="overrideBehaviour">The override behaviour. Specifies whether the local values should override the remote values
    /// or local values should only be used when a remote value doesn't exist or the local values should be used only.</param>
    /// <returns>The new <see cref="FlagOverrides"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static FlagOverrides LocalDictionary(IDictionary<string, object> dictionary, OverrideBehaviour overrideBehaviour) =>
        new(dictionary ?? throw new ArgumentNullException(nameof(dictionary)), false, overrideBehaviour);

    /// <summary>
    /// Creates an instance of the <see cref="FlagOverrides"/> class that uses a dictionary data source.
    /// </summary>
    /// <param name="dictionary">The dictionary that contains the overrides.</param>
    /// <param name="watchChanges">If set to <see langword="true"/>, the input dictionary will be tracked for changes.</param>
    /// <param name="overrideBehaviour">The override behaviour. Specifies whether the local values should override the remote values
    /// or local values should only be used when a remote value doesn't exist or the local values should be used only.</param>
    /// <returns>The new <see cref="FlagOverrides"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static FlagOverrides LocalDictionary(IDictionary<string, object> dictionary, bool watchChanges, OverrideBehaviour overrideBehaviour) =>
        new(dictionary ?? throw new ArgumentNullException(nameof(dictionary)), watchChanges, overrideBehaviour);
}
