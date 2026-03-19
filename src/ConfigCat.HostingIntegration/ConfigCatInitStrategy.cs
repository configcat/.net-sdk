namespace ConfigCat.HostingIntegration;

public enum ConfigCatInitStrategy
{
    DoNotCreateClients = -1,
    DoNotWaitForClientReady,
    WaitForClientReadyAndLogOnFailure,
    WaitForClientReadyAndThrowOnFailure,
}
