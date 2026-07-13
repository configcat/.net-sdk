namespace ConfigCat.Extensions.Hosting.Tests.Helpers;

internal static class ClientConfigurationHelper
{
    private static int Counter;

    public static string NewSdkKey(bool ensureNonExistent = false)
    {
        var n = System.Threading.Interlocked.Increment(ref Counter);
        return $"configcat-sdk-1/fake-{n:D17}/{(ensureNonExistent ? "~~~~~~~~~~~~~~~~~~~~~~" : "1234567890123456789012")}";
    }
}
