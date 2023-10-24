using System.Collections.Generic;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation;

internal struct EvaluateContext
{
    public readonly string Key;
    public readonly Setting Setting;
    public readonly User? User;
    public readonly IReadOnlyDictionary<string, Setting> Settings;

    private IReadOnlyDictionary<string, string>? userAttributes;
    public IReadOnlyDictionary<string, string>? UserAttributes => this.userAttributes ??= this.User?.GetAllAttributes();

    private List<string>? visitedFlags;
    public List<string> VisitedFlags => this.visitedFlags ??= new List<string>();

    public bool IsMissingUserObjectLogged;
    public bool IsMissingUserObjectAttributeLogged;

    public IndentedTextBuilder? LogBuilder;

    public EvaluateContext(string key, Setting setting, User? user, IReadOnlyDictionary<string, Setting> settings)
    {
        this.Key = key;
        this.Setting = setting;
        this.User = user;
        this.Settings = settings;

        this.userAttributes = null;
        this.visitedFlags = null;
        this.IsMissingUserObjectLogged = this.IsMissingUserObjectAttributeLogged = false;
        this.LogBuilder = null; // initialized by RolloutEvaluator.Evaluate
    }

    public EvaluateContext(string key, Setting setting, ref EvaluateContext dependentFlagContext)
        : this(key, setting, dependentFlagContext.User, dependentFlagContext.Settings)
    {
        this.userAttributes = dependentFlagContext.userAttributes;
        this.visitedFlags = dependentFlagContext.VisitedFlags; // crucial to use the property here to make sure the list is created!
        this.LogBuilder = dependentFlagContext.LogBuilder;
    }
}
