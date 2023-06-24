namespace ConfigCat.Client.Evaluation;

internal readonly struct EvaluateContext
{
    public EvaluateContext(string key, Setting setting, string? logDefaultValue, User? user)
    {
        Key = key;
        Setting = setting;
        User = user;
        Log = new EvaluateLogger
        {
            ReturnValue = logDefaultValue,
            User = user,
            KeyName = key,
            VariationId = null
        };
    }

    public string Key { get; }
    public Setting Setting { get; }
    public User? User { get; }
    public EvaluateLogger Log { get; }
}
