using System;
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
/// <remarks>
/// Please note that the <see cref="User"/> class is not designed to be used as a DTO (data transfer object).
/// (Since the type of the <see cref="Custom"/> property is polymorphic, it's not guaranteed that deserializing a serialized instance produces an instance with an identical or even valid data content.)
/// </remarks>
public class User
{
    internal const string DefaultIdentifierValue = "";

    /// <summary>
    /// The unique identifier of the user or session (e.g. email address, primary key, session ID, etc.)
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Email address of the user.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Country of the user.
    /// </summary>
    public string? Country { get; set; }

    private IDictionary<string, object>? custom;

    /// <summary>
    /// Custom attributes of the user for advanced targeting rule definitions (e.g. user role, subscription type, etc.)
    /// </summary>
    /// <remarks>
    /// All comparators support <see cref="string"/> values as User Object attribute (in some cases they need to be provided in a specific format though, see below),<br/>
    /// but some of them also support other types of values. It depends on the comparator how the values will be handled. The following rules apply:
    /// <para>
    /// Text-based comparators (EQUALS, IS ONE OF, etc.)<br/>
    /// * accept <see cref="string"/> values,<br/>
    /// * all other values are automatically converted to <see cref="string"/> (a warning will be logged but evaluation will continue as normal).
    /// </para>
    /// <para>
    /// SemVer-based comparators (IS ONE OF, &lt;, &gt;=, etc.)<br/>
    /// * accept <see cref="string"/> values containing a properly formatted, valid semver value,<br/>
    /// * all other values are considered invalid (a warning will be logged and the currently evaluated targeting rule will be skipped).
    /// </para>
    /// <para>
    /// Number-based comparators (=, &lt;, &gt;=, etc.)<br/>
    /// * accept <see cref="double"/> values and all other numeric values which can safely be converted to <see cref="double"/>,<br/>
    /// * accept <see cref="string"/> values containing a properly formatted, valid <see cref="double"/> value,<br/>
    /// * all other values are considered invalid (a warning will be logged and the currently evaluated targeting rule will be skipped).
    /// </para>
    /// <para>
    /// Date time-based comparators (BEFORE / AFTER)<br/>
    /// * accept <see cref="DateTime"/> or <see cref="DateTimeOffset"/> values, which are automatically converted to a second-based Unix timestamp,<br/>
    /// * accept <see cref="double"/> values representing a second-based Unix timestamp and all other numeric values which can safely be converted to <see cref="double"/>,<br/>
    /// * accept <see cref="string"/> values containing a properly formatted, valid <see cref="double"/> value,<br/>
    /// * all other values are considered invalid (a warning will be logged and the currently evaluated targeting rule will be skipped).
    /// </para>
    /// <para>
    /// String array-based comparators (ARRAY CONTAINS ANY OF / ARRAY NOT CONTAINS ANY OF)<br/>
    /// * accept arrays of <see cref="string"/>,<br/>
    /// * accept <see cref="string"/> values containing a valid JSON string which can be deserialized to an array of <see cref="string"/>,<br/>
    /// * all other values are considered invalid (a warning will be logged and the currently evaluated targeting rule will be skipped).
    /// </para>
    /// </remarks>
    public IDictionary<string, object> Custom
    {
        get => this.custom ??= new Dictionary<string, object>();
        set => this.custom = value;
    }
    /// <summary>
    /// Returns all attributes of the user.
    /// </summary>
    public IReadOnlyDictionary<string, object> GetAllAttributes()
    {
        var result = new Dictionary<string, object>();

        result[nameof(Identifier)] = Identifier;

        if (Email is not null)
        {
            result[nameof(Email)] = Email;
        }

        if (Country is not null)
        {
            result[nameof(Country)] = Country;
        }

        if (this.custom is { Count: > 0 })
        {
            foreach (var item in this.custom)
            {
                if (item.Value is not null && item.Key is not (nameof(Identifier) or nameof(Email) or nameof(Country)))
                {
                    result.Add(item.Key, item.Value);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="identifier">The unique identifier of the user or session.</param>
    public User(string identifier)
    {
        Identifier = string.IsNullOrEmpty(identifier) ? DefaultIdentifierValue : identifier;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Identifier;
    }
}
