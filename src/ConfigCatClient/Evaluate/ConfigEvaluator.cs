using System;
using System.Collections.Generic;
using ConfigCat.Client.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigCat.Client.Evaluate
{
    internal class ConfigEvaluator : IConfigEvaluator
    {
        private ILogger log;

        public ConfigEvaluator(ILogger logger)
        {
            this.log = logger;
        }

        public T GetValue<T>(ProjectConfig projectConfig, string key, T defaultValue, User user = null)
        {
            if (projectConfig.JsonString == null)
            {
                this.log.Warning("ConfigJson is not present, returning defaultValue");

                return defaultValue;
            }

            var json = (Dictionary<string, Setting>)JsonConvert.DeserializeObject(projectConfig.JsonString);

            Setting setting;

            if (!json.TryGetValue(key, out setting))           
            {
                this.log.Warning($"Unknown key: '{key}'");

                return defaultValue;
            }

            if (user != null)
            {
                // evaluation logic                
            }
            else
            {
                // regular evaluation
            }

            throw new NotImplementedException();
        }
    }

    internal class Setting
    {
        public string Value { get; set; }

        public SettingType SettingType { get; set; }

        public List<RolloutPercentageItem> RolloutPercentageItems { get; set; }

        public List<RolloutRule> RolloutRules { get; set; }
    }

    internal class RolloutPercentageItem
    {
        public int Order { get; set; }

        [JsonProperty(PropertyName ="Value")]
        public string RawValue { get; private set; }

        public double Percentage { get; set; }

        public bool GetValueAsBoolean()
        {
            return bool.Parse(this.RawValue);
        }

        public string GetValueAsString()
        {
            return this.RawValue;
        }

        public int GetValueAsInteger()
        {
            return int.Parse(this.RawValue);
        }

        public double GetValueAsDouble()
        {
            return double.Parse(this.RawValue);
        }        
    }

    internal class RolloutRule
    {
        public short Order { get; set; }

        public string ComparisonAttribute { get; set; }

        public byte Comparator { get; set; }

        public string ComparisonValue { get; set; }

        [JsonProperty(PropertyName = "Value")]
        public string RawValue { get; private set; }         
    }

    internal enum SettingType : byte
    {
        Boolean = 0,
        
        String = 1,
        
        Int = 2,
        
        Double = 3
    }
}