namespace ConfigCat.Client;

internal sealed class NullHooks : Hooks
{
    public static readonly NullHooks Instance = new();

    private NullHooks() : base(new Events()) { }

    public override bool TryDisconnect()
    {
        return false;
    }

    public override IConfigCatClient? Sender
    {
        get => null;
        set { /* this is an intentional no-op */  }
    }
}
