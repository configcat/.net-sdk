namespace ConfigCat.HostingIntegration;

public enum ConfigCatInitStrategy
{
    DoNotInitializeClients,
    DoNotWaitForClientReady,
    WaitForClientReadyAndLogOnFailure,
    WaitForClientReadyAndThrowOnFailure,
}
