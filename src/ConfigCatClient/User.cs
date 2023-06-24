using System.Collections.Generic;

#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

/// <summary>
/// User Object. Contains user attributes which are used for evaluating targeting rules and percentage options.
/// </summary>
public class User
{
    internal const string DefaultIdentifierValue = "";

    /// <summary>
    /// The unique identifier of the user or session (e.g. email address, primary key, session ID, etc.)
    /// </summary>
    public string Identifier { get; private set; }

    /// <summary>
    /// Email address of the user.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Country of the user.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Custom attributes of the user for advanced targeting rule definitions (e.g. user role, subscription type, etc.)
    /// </summary>
    public IDictionary<string, string?> Custom { get; set; }

    /// <summary>
    /// Returns all attributes of the user.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string?> AllAttributes
    {
        get
        {
            var result = new Dictionary<string, string?>
            {
                { nameof(Identifier), Identifier},
                { nameof(Email), Email},
                { nameof(Country),  Country},
            };

            if (Custom is not { Count: > 0 })
                return result;

            foreach (var item in Custom)
            {
                if (item.Key is not (nameof(Identifier) or nameof(Email) or nameof(Country)))
                {
                    result.Add(item.Key, item.Value);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="identifier">The unique identifier of the user or session.</param>
    public User(string identifier)
    {
        Identifier = string.IsNullOrEmpty(identifier) ? DefaultIdentifierValue : identifier;
        Custom = new Dictionary<string, string?>(capacity: 0);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Identifier;
    }
}
