using System.Collections.Generic;
using System.Text;

namespace ConfigCat.Client.Evaluate
{
    internal sealed class EvaluateLogger<T>
    {
        public User User { get; set; }

        public T ReturnValue { get; set; }

        public string KeyName { get; set; }

        public ICollection<string> Operations { get; private set; } = new List<string>();

        public void Log(string message)
        {
            this.Operations.Add(message);
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            result.AppendLine($" Evaluate '{KeyName}'");

            result.AppendLine($" User object: {Newtonsoft.Json.JsonConvert.SerializeObject(this.User)}");

            foreach (var o in this.Operations)
            {
                result.AppendLine(" " + o);
            }

            result.AppendLine($" Returning: {this.ReturnValue}");

            return result.ToString();
        }

        public static string FormatComparator(ComparatorEnum comparator)
        {
            switch (comparator)
            {
                case ComparatorEnum.In:
                case ComparatorEnum.SemVerIn:
                    return "IS ONE OF";
                case ComparatorEnum.NotIn:
                case ComparatorEnum.SemVerNotIn:
                    return "IS NOT ONE OF";
                case ComparatorEnum.Contains:
                    return "CONTAINS";
                case ComparatorEnum.NotContains:
                    return "DOES NOT CONTAIN";
                case ComparatorEnum.SemVerLessThan:
                case ComparatorEnum.NumberLessThan:
                    return "<";
                case ComparatorEnum.SemVerLessThanEqual:
                case ComparatorEnum.NumberLessThanEqual:
                    return "<=";
                case ComparatorEnum.SemVerGreaterThan:
                case ComparatorEnum.NumberGreaterThan:
                    return ">";
                case ComparatorEnum.SemVerGreaterThanEqual:
                case ComparatorEnum.NumberGreaterThanEqual:
                    return ">=";
                case ComparatorEnum.NumberEqual:
                    return "=";
                case ComparatorEnum.NumberNotEqual:
                    return "!=";
                case ComparatorEnum.SensitiveOneOf:
                    return "IS ONE OF (Sensitive)";
                case ComparatorEnum.SensitiveNotOneOf:
                    return "IS NOT ONE OF (Sensitive)";
                default:
                    return comparator.ToString();
            }
        }
    }
}
