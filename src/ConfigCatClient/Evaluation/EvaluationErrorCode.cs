namespace ConfigCat.Client;

/// <summary>
/// Specifies the possible evaluation error codes.
/// </summary>
public enum EvaluationErrorCode
{
    /// <summary>
    /// An unexpected error occurred during the evaluation.
    /// </summary>
    UnexpectedError = -1,
    /// <summary>
    /// No error occurred (the evaluation was successful).
    /// </summary>
    None = 0,
    /// <summary>
    /// The evaluation failed because of an error in the config model. (Most likely, invalid data was passed to the SDK via flag overrides.)
    /// </summary>
    InvalidConfigModel = 1,
    /// <summary>
    /// The evaluation failed because of a type mismatch between the evaluated setting value and the specified default value.
    /// </summary>
    SettingValueTypeMismatch = 2,
    /// <summary>
    /// The evaluation failed because the config JSON was not available locally.
    /// </summary>
    ConfigJsonNotAvailable = 1000,
    /// <summary>
    /// The evaluation failed because the key of the evaluated setting was not found in the config JSON.
    /// </summary>
    SettingKeyMissing = 1001,
}
