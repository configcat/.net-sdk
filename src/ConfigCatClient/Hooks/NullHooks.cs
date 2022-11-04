using System;

namespace ConfigCat.Client
{
    internal sealed class NullHooks : Hooks
    {
        public static readonly NullHooks Instance = new();

        private NullHooks() : base(new EventHandlers()) { }

        public override bool TryDisconnect(out Action<IConfigCatClient> raiseBeforeClientDispose)
        {
            raiseBeforeClientDispose = default;
            return false;
        }

        public override void SetSender(IConfigCatClient client) { /* this is an intentional no-op */ }
    }
}
