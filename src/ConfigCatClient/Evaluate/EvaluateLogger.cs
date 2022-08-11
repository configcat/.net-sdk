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

            result.AppendLine($"Evaluating '{KeyName}'");
            foreach (var o in this.Operations)
            {
                result.AppendLine(" " + o);
            }
            result.Append($" Returning '{this.ReturnValue}' (VariationId: '{this.VariationId ?? "null"}').");

            return result.ToString();
        }

        public static string FormatComparator(Comparator comparator)
        {
            return comparator switch
            {
                Comparator.In => "IS ONE OF",
                Comparator.SemVerIn => "IS ONE OF",
                Comparator.NotIn => "IS NOT ONE OF",
                Comparator.SemVerNotIn => "IS NOT ONE OF",
                Comparator.Contains => "CONTAINS",
                Comparator.NotContains => "DOES NOT CONTAIN",
                Comparator.SemVerLessThan => "<",
                Comparator.NumberLessThan => "<",
                Comparator.SemVerLessThanEqual => "<=",
                Comparator.NumberLessThanEqual => "<=",
                Comparator.SemVerGreaterThan => ">",
                Comparator.NumberGreaterThan => ">",
                Comparator.SemVerGreaterThanEqual => ">=",
                Comparator.NumberGreaterThanEqual => ">=",
                Comparator.NumberEqual => "=",
                Comparator.NumberNotEqual => "!=",
                Comparator.SensitiveOneOf => "IS ONE OF (hashed)",
                Comparator.SensitiveNotOneOf => "IS NOT ONE OF (hashed)",
                _ => comparator.ToString()
            };
        }
    }
}
