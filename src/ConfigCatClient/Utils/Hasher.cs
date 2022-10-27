using System;
using System.Security.Cryptography;
using System.Text;

namespace ConfigCat.Client.Utils
{
    // NOTE: Instance methods of HashAlgorithm are not thread-safe (see https://stackoverflow.com/a/26592826/8656352),
    // so we can't really use a shared HashAlgorithm instance cached in e.g. an (ordinary) static field or a long-lived object.
    // .NET 5, however, introduced new static helpers which are thread-safe (see https://github.com/dotnet/runtime/issues/17590 and
    // https://stackoverflow.com/a/74211393/8656352), so we hide hashing behind this zero cost abstraction to benefit from the new methods.
    internal readonly struct Hasher : IDisposable
    {
#if NET5_0_OR_GREATER
        public static Hasher Create() => default;

        public void Dispose() { /* for SonarQube™: this is an intentional no-op! */ }

        public byte[] Hash(byte[] bytes) => SHA1.HashData(bytes);
#else
        public static Hasher Create() => new(SHA1.Create());

        private readonly HashAlgorithm hasher;

        private Hasher(HashAlgorithm hasher) => this.hasher = hasher;

        public void Dispose() => this.hasher?.Dispose();

        public byte[] Hash(byte[] bytes) => (this.hasher ?? throw new InvalidOperationException()).ComputeHash(bytes);
#endif

        public string Hash(string text) => Hash(Encoding.UTF8.GetBytes(text)).ToHexString();
    }
}
