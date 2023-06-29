using System.Collections.Generic;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation;

internal struct EvaluateContext
{
    public EvaluateContext(string key, Setting setting, SettingValue defaultValue, User? user)
    {
        this.Key = key;
        this.Setting = setting;
        this.DefaultValue = defaultValue;
        this.User = user;
        this.userAttributes = null;
        this.visitedFlags = null;
        this.IsMissingUserObjectLogged = this.IsMissingUserObjectAttributeLogged = false;
        this.LogBuilder = null; // initialized by RolloutEvaluator.Evaluate
    }

    public readonly string Key;
    public readonly Setting Setting;
    public readonly SettingValue DefaultValue;
    public readonly User? User;

    private IReadOnlyDictionary<string, string>? userAttributes;
    public IReadOnlyDictionary<string, string>? UserAttributes => this.userAttributes ??= this.User?.GetAllAttributes();

    private List<string>? visitedFlags;
    public List<string> VisitedFlags => this.visitedFlags ??= new List<string>();

    public bool IsMissingUserObjectLogged;
    public bool IsMissingUserObjectAttributeLogged;

    public IndentedTextBuilder? LogBuilder;
}
