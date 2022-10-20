using ConfigCat.Client.Override;
using System;
using System.Collections.Generic;

namespace ConfigCat.Client
{
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
            this.OverrideBehaviour = overrideBehaviour;
        }

        private FlagOverrides(IDictionary<string, object> dictionary, OverrideBehaviour overrideBehaviour)
        {
            this.dictionary = dictionary;
            this.OverrideBehaviour = overrideBehaviour;
        }


        /// <summary>
        /// The override behaviour. It can be used to set preference on whether the local values should
        /// override the remote values, or use local values only when a remote value doesn't exist,
        /// or use it for local only mode.
        /// </summary>
        public OverrideBehaviour OverrideBehaviour { get; private set; }

        internal IOverrideDataSource BuildDataSource(LoggerWrapper logger, WeakReference<IConfigCatClient> clientWeakRef)
        {
            if (this.dictionary != null)
                return new LocalDictionaryDataSource(this.dictionary);

            if (this.filePath != null)
                return new LocalFileDataSource(this.filePath, this.autoReload, logger, clientWeakRef);

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
            new FlagOverrides(filePath, autoReload, overrideBehaviour);

        /// <summary>
        /// Creates an override descriptor that uses a dictionary data source.
        /// </summary>
        /// <param name="dictionary">Dictionary that contains the overrides.</param>
        /// <param name="overrideBehaviour">the override behaviour. It can be used to set preference on whether the local values should override the remote values, or use local values only when a remote value doesn't exist, or use it for local only mode.</param>
        /// <returns>The override descriptor.</returns>
        public static FlagOverrides LocalDictionary(IDictionary<string, object> dictionary, OverrideBehaviour overrideBehaviour) =>
            new FlagOverrides(dictionary, overrideBehaviour);
    }
}
