namespace ConfigCat.Client;

public enum EvaluationErrorCode
{
    UnexpectedError = -1,
    None = 0,
    InvalidConfigModel = 1,
    SettingValueTypeMismatch = 2,
    ConfigJsonNotAvailable = 1000,
    SettingKeyMissing = 1001,
}
