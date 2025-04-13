namespace ConfigCat.Extensions.Hosting;

public enum ConfigCatInitStrategy
{
    DoNotWaitForClientReady,
    WaitForClientReadyAndLogOnFailure,
    WaitForClientReadyAndThrowOnFailure,
}
