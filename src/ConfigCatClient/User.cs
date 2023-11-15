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
    /// The set of allowed attribute values depends on the comparison type of the condition which references the User Object attribute.<br/>
    /// <see cref="string"/> values are supported by all comparison types (in some cases they need to be provided in a specific format though).<br/>
    /// Some of the comparison types work with other types of values, as descibed below.
    /// <para>
    /// Text-based comparisons (EQUALS, IS ONE OF, etc.)<br/>
    /// * accept <see cref="string"/> values,<br/>
    /// * all other values are automatically converted to string (a warning will be logged but evaluation will continue as normal).
    /// </para>
    /// <para>
    /// SemVer-based comparisons (IS ONE OF, &lt;, &gt;=, etc.)<br/>
    /// * accept <see cref="string"/> values containing a properly formatted, valid semver value,<br/>
    /// * all other values are considered invalid (a warning will be logged and the currently evaluated targeting rule will be skipped).
    /// </para>
    /// <para>
    /// Number-based comparisons (=, &lt;, &gt;=, etc.)<br/>
    /// * accept <see cref="double"/> values (except for <see cref="double.NaN"/>) and all other numeric values which can safely be converted to <see cref="double"/>,<br/>
    /// * accept <see cref="string"/> values containing a properly formatted, valid <see cref="double"/> value,<br/>
    /// * all other values are considered invalid (a warning will be logged and the currently evaluated targeting rule will be skipped).
    /// </para>
    /// <para>
    /// Date time-based comparisons (BEFORE / AFTER)<br/>
    /// * accept <see cref="DateTime"/> or <see cref="DateTimeOffset"/> values, which are automatically converted to a second-based Unix timestamp,<br/>
    /// * accept <see cref="double"/> values (except for <see cref="double.NaN"/>) representing a second-based Unix timestamp and all other numeric values which can safely be converted to <see cref="double"/>,<br/>
    /// * accept <see cref="string"/> values containing a properly formatted, valid <see cref="double"/> value,<br/>
    /// * all other values are considered invalid (a warning will be logged and the currently evaluated targeting rule will be skipped).
    /// </para>
    /// <para>
    /// String array-based comparisons (ARRAY CONTAINS ANY OF / ARRAY NOT CONTAINS ANY OF)<br/>
    /// * accept arrays of <see cref="string"/>,<br/>
    /// * accept <see cref="string"/> values containing a valid JSON string which can be deserialized to an array of <see cref="string"/>,<br/>
    /// * all other values are considered invalid (a warning will be logged and the currently evaluated targeting rule will be skipped).
    /// </para>
    /// In case a non-string attribute value needs to be converted to <see cref="string"/> during evaluation, it will always be done using the same format which is accepted by the comparisons.
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
