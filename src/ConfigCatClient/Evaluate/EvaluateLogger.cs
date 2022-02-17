using System;
using System.Collections.Generic;
using System.Text;

namespace ConfigCat.Client.Evaluate
{
    internal sealed class EvaluateLogger<T>
    {
        public User User { get; set; }

        public T ReturnValue { get; set; }

        public string KeyName { get; set; }

        private ICollection<string> Operations { get; set; } = new List<string>();

        public string VariationId { get; set; }

        public void Log(string message)
        {
            this.Operations.Add(message);
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            result.AppendLine($" Evaluate '{KeyName}'");

            result.AppendLine($" VariationId: {this.VariationId ?? "null"}");

            result.AppendLine($" User object: {this.User.Serialize()}");

            foreach (var o in this.Operations)
            {
                result.AppendLine(" " + o);
            }

            result.AppendLine($" Returning: {this.ReturnValue}");

            return result.ToString();
        }

        public static string FormatComparator(ComparatorEnum comparator)
        {
            return comparator switch
            {
                ComparatorEnum.In => "IS ONE OF",
                ComparatorEnum.SemVerIn => "IS ONE OF",
                ComparatorEnum.NotIn => "IS NOT ONE OF",
                ComparatorEnum.SemVerNotIn => "IS NOT ONE OF",
                ComparatorEnum.Contains => "CONTAINS",
                ComparatorEnum.NotContains => "DOES NOT CONTAIN",
                ComparatorEnum.SemVerLessThan => "<",
                ComparatorEnum.NumberLessThan => "<",
                ComparatorEnum.SemVerLessThanEqual => "<=",
                ComparatorEnum.NumberLessThanEqual => "<=",
                ComparatorEnum.SemVerGreaterThan => ">",
                ComparatorEnum.NumberGreaterThan => ">",
                ComparatorEnum.SemVerGreaterThanEqual => ">=",
                ComparatorEnum.NumberGreaterThanEqual => ">=",
                ComparatorEnum.NumberEqual => "=",
                ComparatorEnum.NumberNotEqual => "!=",
                ComparatorEnum.SensitiveOneOf => "IS ONE OF (Sensitive)",
                ComparatorEnum.SensitiveNotOneOf => "IS NOT ONE OF (Sensitive)",
                _ => comparator.ToString()
            };
        }
    }
}
