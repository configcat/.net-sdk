using ConfigCat.Client.Evaluate;
using Newtonsoft.Json;
using System;
using System.Data.HashFunction;
using System.Data.HashFunction.MurmurHash;
using System.IO;

namespace ConfigCat.Client
{
    internal class ConfigDeserializer : IConfigDeserializer
    {
        private readonly IHashFunction hasher = MurmurHash3Factory.Instance.Create(new MurmurHash3Config{ HashSizeInBits = 128 });
        private readonly ILogger logger;
        private readonly JsonSerializer serializer;
        private SettingsWithPreferences lastSerializedSettings;
        private byte[] hashToCompare;

        public ConfigDeserializer(ILogger logger, JsonSerializer serializer)
        {
            this.logger = logger;
            this.serializer = serializer;
        }

        public bool TryDeserialize(string config, out SettingsWithPreferences settings)
        {
            if (config == null)
            {
                this.logger.Warning("ConfigJson is not present.");

                settings = null;

                return false;
            }

            var hash = this.hasher.ComputeHash(config).Hash;
            if(CompareByteArrays(this.hashToCompare, hash))
            {
                settings = this.lastSerializedSettings;
                return true;
            }

            using (var stringReader = new StringReader(config))
            using (var reader = new JsonTextReader(stringReader))
            { 
                this.lastSerializedSettings = settings = this.serializer.Deserialize<SettingsWithPreferences>(reader);
            }

            this.hashToCompare = hash;
            return true;
        }

        private static bool CompareByteArrays(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2) => b1.SequenceEqual(b2);
    }
}
