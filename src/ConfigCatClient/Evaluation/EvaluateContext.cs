using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation;

internal struct EvaluateContext
{
    public readonly string Key;
    public readonly Setting Setting;
    public readonly IReadOnlyDictionary<string, Setting> Settings;

    private readonly User? user;

    [MemberNotNullWhen(true, nameof(UserAttributes))]
    public readonly bool IsUserAvailable => this.user is not null;

    private IReadOnlyDictionary<string, object>? userAttributes;
    public IReadOnlyDictionary<string, object>? UserAttributes => this.userAttributes ??= this.user?.GetAllAttributes();

    private List<string>? visitedFlags;
    public List<string> VisitedFlags => this.visitedFlags ??= new List<string>();

    public bool IsMissingUserObjectLogged;
    public bool IsMissingUserObjectAttributeLogged;

    public IndentedTextBuilder? LogBuilder;

    public EvaluateContext(string key, Setting setting, User? user, IReadOnlyDictionary<string, Setting> settings)
    {
        this.Key = key;
        this.Setting = setting;
        this.user = user;
        this.Settings = settings;

        this.userAttributes = null;
        this.visitedFlags = null;
        this.IsMissingUserObjectLogged = this.IsMissingUserObjectAttributeLogged = false;
        this.LogBuilder = null; // initialized by RolloutEvaluator.Evaluate
    }

    public EvaluateContext(string key, Setting setting, ref EvaluateContext dependentFlagContext)
        : this(key, setting, dependentFlagContext.user, dependentFlagContext.Settings)
    {
        this.userAttributes = dependentFlagContext.UserAttributes;
        this.visitedFlags = dependentFlagContext.VisitedFlags; // crucial to use the property here to make sure the list is created!
        this.LogBuilder = dependentFlagContext.LogBuilder;
    }
}
