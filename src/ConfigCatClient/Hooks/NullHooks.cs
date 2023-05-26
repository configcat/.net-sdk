namespace ConfigCat.Client;

internal sealed class NullHooks : Hooks
{
    public static readonly NullHooks Instance = new();

    private NullHooks() : base(new EventHandlers()) { }

    public override bool TryDisconnect()
    {
        return false;
    }

    public override void SetSender(IConfigCatClient client) { /* this is an intentional no-op */ }
}
