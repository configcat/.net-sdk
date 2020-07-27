namespace ConfigCat.Client.Evaluate
{
    internal class EvaluateResult
    {
        public string RawValue { get; set; }

        public string VariationId { get; set; }

        public EvaluateResult() : this(null, null) { }

        public EvaluateResult(string rawValue, string variationId)
        {
            this.RawValue = rawValue;
            this.VariationId = variationId;
        }
    }
}