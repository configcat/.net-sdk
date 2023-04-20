using System.Collections.Generic;
using System.Text;

namespace ConfigCat.Client.Evaluation;

internal sealed class EvaluateLogger
{
    public User? User { get; set; }

    public string? ReturnValue { get; set; }

    public string KeyName { get; set; } = null!;

    private ICollection<string> Operations { get; } = new List<string>();

    public string? VariationId { get; set; }

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
