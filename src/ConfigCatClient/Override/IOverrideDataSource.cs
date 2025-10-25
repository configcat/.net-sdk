using System.Collections.Generic;

namespace ConfigCat.Client.Override;

/// <summary>
/// Defines the interface used by the ConfigCat SDK to obtain flag overrides.
/// </summary>
public interface IOverrideDataSource
{
    /// <summary>
    /// Returns the flag overrides as a <see cref="Dictionary{TKey, TValue}"/> where the dictionary key is the feature flag key.
    /// </summary>
    /// <remarks>
    /// Note for implementers. Mutating the returned dictionary results in undefined behavior, thus, it must be avoided.
    /// </remarks>
    /// <returns>The dictionary of flag overrides.</returns>
    IReadOnlyDictionary<string, Setting> GetOverrides();
}
