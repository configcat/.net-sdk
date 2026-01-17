namespace ConfigCat.HostingIntegration;

public enum ConfigCatInitStrategy
{
    DoNotWaitForClientReady,
    WaitForClientReadyAndLogOnFailure,
    WaitForClientReadyAndThrowOnFailure,
}
