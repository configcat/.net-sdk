namespace ConfigCat.Client;

/// <summary>
/// Setting type.
/// </summary>
public enum SettingType : byte
{
    /// <summary>
    /// On/off type (feature flag).
    /// </summary>
    Boolean = 0,
    /// <summary>
    /// Text type.
    /// </summary>
    String = 1,
    /// <summary>
    /// Whole number type.
    /// </summary>
    Int = 2,
    /// <summary>
    /// Decimal number type.
    /// </summary>
    Double = 3,
    /// <summary>
    /// Unknown type.
    /// </summary>
    Unknown = byte.MaxValue,
}
