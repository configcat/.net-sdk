﻿using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
#if NET45
            // TLS 1.2 was not enabled before .NET 4.6 by default (see https://stackoverflow.com/a/58195987/8656352),
            // so we need to do this because CDN servers are configured to require TLS 1.2+ currently.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif
        }
    }
}
