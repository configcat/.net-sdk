using System;

namespace ConfigCat.Client
{
    internal sealed class NullHooks : Hooks
    {
        private static void Noop(Delegate eventHandler) { /* This method is for keeping SonarQube happy. */ }

        public static readonly NullHooks Instance = new();

        public override event EventHandler ClientReady
        { 
            add { Noop(value); } 
            remove { Noop(value); }
        }

        public override event EventHandler<FlagEvaluatedEventArgs> FlagEvaluated
        {
            add { Noop(value); }
            remove { Noop(value); }
        }

        public override event EventHandler<ConfigChangedEventArgs> ConfigChanged
        {
            add { Noop(value); }
            remove { Noop(value); }
        }

        public override event EventHandler<ConfigCatClientErrorEventArgs> Error
        {
            add { Noop(value); }
            remove { Noop(value); }
        }

        public override event EventHandler BeforeClientDispose
        {
            add { Noop(value); }
            remove { Noop(value); }
        }
    }
}
