using System;
using System.Collections.Generic;
using ConfigCat.Client.Override;

namespace ConfigCat.Client;

/// <summary>
/// Describes feature flag and setting overrides.
/// </summary>
public class FlagOverrides
{
    private readonly string filePath;
    private readonly bool autoReload;

    private readonly IDictionary<string, object> dictionary;

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
    /// The override behaviour. It can be used to set preference on whether the local values should
    /// override the remote values, or use local values only when a remote value doesn't exist,
    /// or use it for local only mode.
    /// </summary>
    public OverrideBehaviour OverrideBehaviour { get; private set; }

    internal IOverrideDataSource BuildDataSource(LoggerWrapper logger)
    {
        if (this.dictionary is not null)
            return new LocalDictionaryDataSource(this.dictionary, this.autoReload);

        if (this.filePath is not null)
            return new LocalFileDataSource(this.filePath, this.autoReload, logger);

        throw new InvalidOperationException("Could not determine the right override data source.");
    }

    /// <summary>
    /// Creates an override descriptor that uses a local file data source.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="autoReload">When it's true, the file will be reloaded when it gets modified.</param>
    /// <param name="overrideBehaviour">the override behaviour. It can be used to set preference on whether the local values should override the remote values, or use local values only when a remote value doesn't exist, or use it for local only mode.</param>
    /// <returns>The override descriptor.</returns>
    public static FlagOverrides LocalFile(string filePath, bool autoReload, OverrideBehaviour overrideBehaviour) =>
        new(filePath, autoReload, overrideBehaviour);

    /// <summary>
    /// Creates an override descriptor that uses a dictionary data source.
    /// </summary>
    /// <param name="dictionary">Dictionary that contains the overrides.</param>
    /// <param name="overrideBehaviour">the override behaviour. It can be used to set preference on whether the local values should override the remote values, or use local values only when a remote value doesn't exist, or use it for local only mode.</param>
    /// <returns>The override descriptor.</returns>
    public static FlagOverrides LocalDictionary(IDictionary<string, object> dictionary, OverrideBehaviour overrideBehaviour) =>
        new(dictionary, false, overrideBehaviour);

    /// <summary>
    /// Creates an override descriptor that uses a dictionary data source.
    /// </summary>
    /// <param name="dictionary">Dictionary that contains the overrides.</param>
    /// <param name="watchChanges">Indicates whether the SDK should track the input dictionary for changes.</param>
    /// <param name="overrideBehaviour">the override behaviour. It can be used to set preference on whether the local values should override the remote values, or use local values only when a remote value doesn't exist, or use it for local only mode.</param>
    /// <returns>The override descriptor.</returns>
    public static FlagOverrides LocalDictionary(IDictionary<string, object> dictionary, bool watchChanges, OverrideBehaviour overrideBehaviour) =>
        new(dictionary, watchChanges, overrideBehaviour);
}
