namespace ConfigCat.Client.Evaluate
{
    internal interface IRolloutEvaluator
    {
        T Evaluate<T>(ProjectConfig projectConfig, string key, T defaultValue, User user = null);
    }
}