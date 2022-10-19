using System;
using ConfigCat.Client.Evaluation;

#if USE_NEWTONSOFT_JSON
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client
{
    internal delegate EvaluationDetails EvaluationDetailsFactory(SettingType settingType, JsonValue value);

    /// <summary>
    /// The evaluated value and additional information about the evaluation of a feature or setting flag.
    /// </summary>
    public abstract record class EvaluationDetails
    {
        internal static EvaluationDetails<TValue> Create<TValue>(JsonValue value)
        {
            return new EvaluationDetails<TValue>
            {

#if USE_NEWTONSOFT_JSON
                Value = Newtonsoft.Json.Linq.Extensions.Value<TValue>(value)
#else
                Value = System.Text.Json.JsonSerializer.Deserialize<TValue>(value)
#endif
            };
        }

        internal static EvaluationDetails Create(SettingType settingType, JsonValue value)
        {
            return settingType switch
            {
                SettingType.Boolean => Create<bool>(value),
                SettingType.String => Create<string>(value),
                SettingType.Int => Create<long>(value),
                SettingType.Double => Create<double>(value),
                _ => throw new ArgumentOutOfRangeException(nameof(settingType), settingType, null)
            };
        }

        internal static EvaluationDetails FromJsonValue(
            EvaluationDetailsFactory factory,
            SettingType settingType,
            string key,
            JsonValue value,
            string variationId,
            DateTime? fetchTime,
            User user,
            RolloutRule matchedEvaluationRule = null,
            RolloutPercentageItem matchedEvaluationPercentageRule = null)
        {
            var instance = factory(settingType, value);

            instance.Key = key;
            instance.VariationId = variationId;
            if (fetchTime is not null)
            {
                instance.FetchTime = fetchTime.Value;
            }
            instance.User = user;
            instance.MatchedEvaluationRule = matchedEvaluationRule;
            instance.MatchedEvaluationPercentageRule = matchedEvaluationPercentageRule;

            return instance;
        }

        internal static EvaluationDetails<TValue> FromDefaultValue<TValue>(string key, TValue defaultValue, DateTime? fetchTime, User user,
            string errorMessage = null, Exception errorException = null)
        {
            var instance = new EvaluationDetails<TValue>
            {
                Key = key,
                Value = defaultValue,
                User = user,
                IsDefaultValue = true,
                ErrorMessage = errorMessage,
                ErrorException = errorException
            };

            if (fetchTime is not null)
            {
                instance.FetchTime = fetchTime.Value;
            }

            return instance;
        }

        internal static EvaluationDetails FromDefaultVariationId(string key, string defaultVariationId, DateTime? fetchTime, User user,
            string errorMessage = null, Exception errorException = null)
        {
            var instance = new EvaluationDetails<object>
            {
                Key = key,
                User = user,
                IsDefaultValue = true,
                VariationId = defaultVariationId,
                ErrorMessage = errorMessage,
                ErrorException = errorException
            };

            if (fetchTime is not null)
            {
                instance.FetchTime = fetchTime.Value;
            }

            return instance;
        }

        private protected EvaluationDetails() { }

        /// <summary>
        /// Key of the feature or setting flag.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Evaluated value of the feature or setting flag.
        /// </summary>
        public object Value => GetValueAsObject();

        private protected abstract object GetValueAsObject();

        /// <summary>
        /// Variation ID of the feature or setting flag (if available).
        /// </summary>
        public string VariationId { get; set; }

        /// <summary>
        /// Time of last successful download of config.json (or <see cref="DateTime.MinValue"/> if there has been no successful download yet).
        /// </summary>
        public DateTime FetchTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// The <see cref="User"/> object used for the evaluation (if available).
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Indicates whether the default value passed to <see cref="IConfigCatClient.GetValue"/> or <see cref="IConfigCatClient.GetValueAsync"/>
        /// is used as the result of the evaluation.
        /// </summary>
        public bool IsDefaultValue { get; set; }

        /// <summary>
        /// Error message in case evaluation failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The <see cref="Exception"/> object related to the error in case evaluation failed (if any).
        /// </summary>
        public Exception ErrorException { get; set; }

        /// <summary>
        /// The comparison-based targeting rule which was used to select the evaluated value (if any).
        /// </summary>
        public RolloutRule MatchedEvaluationRule { get; set; }

        /// <summary>
        /// The percentage-based targeting rule which was used to select the evaluated value (if any).
        /// </summary>
        public RolloutPercentageItem MatchedEvaluationPercentageRule { get; set; }
    }

    /// <inheritdoc/>
    public sealed record class EvaluationDetails<TValue> : EvaluationDetails
    {
        /// <summary>
        /// Creates an instance of <see cref="EvaluationDetails"/>.
        /// </summary>
        public EvaluationDetails() { }

        /// <inheritdoc/>
        public new TValue Value { get; set; }

        private protected override object GetValueAsObject() => Value;
    }
}
