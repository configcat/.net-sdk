namespace ConfigCat.Client.Evaluate
{
    internal interface IConfigEvaluator
    {
        T GetValue<T>(ProjectConfig projectConfig, string key, T defaultValue, User user = null);
    }
}