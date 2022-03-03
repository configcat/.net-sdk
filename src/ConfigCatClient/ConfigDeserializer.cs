using ConfigCat.Client.Evaluate;
using System;
using System.Data.HashFunction;
using System.Data.HashFunction.MurmurHash;

namespace ConfigCat.Client
{
    internal class ConfigDeserializer : IConfigDeserializer
    {
        private readonly IHashFunction hasher = MurmurHash3Factory.Instance.Create(new MurmurHash3Config { HashSizeInBits = 128 });
        private SettingsWithPreferences lastSerializedSettings;
        private byte[] hashToCompare;

        public bool TryDeserialize(string config, out SettingsWithPreferences settings)
        {
            if (config == null)
            {
                settings = null;
                return false;
            }

            var hash = this.hasher.ComputeHash(config).Hash;
            if (CompareByteArrays(this.hashToCompare, hash))
            {
                settings = this.lastSerializedSettings;
                return true;
            }

            this.lastSerializedSettings = settings = config.Deserialize<SettingsWithPreferences>();
            this.hashToCompare = hash;
            return true;
        }

        private static bool CompareByteArrays(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2) => b1.SequenceEqual(b2);
    }
}
