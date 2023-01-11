using System.Collections.Generic;
using System.Text;

namespace ConfigCat.Client.Evaluation;

internal sealed class EvaluateLogger<T>
{
    public User User { get; set; }

    public T ReturnValue { get; set; }

    public string KeyName { get; set; }

    private ICollection<string> Operations { get; set; } = new List<string>();

    public string VariationId { get; set; }

    public void Log(string message)
    {
        Operations.Add(message);
    }

    public override string ToString()
    {
        var result = new StringBuilder();

        result.AppendLine($"Evaluating '{KeyName}'");
        foreach (var o in Operations)
        {
            result.AppendLine("  " + o);
        }
        result.Append($"  Returning '{ReturnValue}' (VariationId: '{VariationId ?? "null"}').");

        return result.ToString();
    }
}
