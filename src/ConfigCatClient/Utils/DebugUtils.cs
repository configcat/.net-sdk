using System;
using System.Diagnostics;

namespace ConfigCat.Client.Utils
{
    internal static class DebugUtils
    {
        [Conditional("DEBUG")]
        public static void Verify(bool condition)
        {
            if (!condition)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw new InvalidOperationException("DEBUG condition failed!");
            }
        }
    }
}
