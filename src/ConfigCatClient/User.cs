using System;
using System.Collections.Generic;
using System.Globalization;
using ConfigCat.Client.Utils;
using System.Linq;

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
    /// <summary>
    /// Converts the specified <see cref="DateTimeOffset"/> value to the format expected by datetime comparison operators (BEFORE/AFTER).
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTimeOffset"/> value to convert.</param>
    /// <returns>The User Object attribute value in the expected format.</returns>
    public static string AttributeValueFrom(DateTimeOffset dateTime)
    {
        var unixTimeSeconds = DateTimeUtils.ToUnixTimeMilliseconds(dateTime.UtcDateTime) / 1000.0;
        return unixTimeSeconds.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the specified <see cref="double"/> value to the format expected by number comparison operators.
    /// </summary>
    /// <param name="number">The <see cref="double"/> value to convert.</param>
    /// <returns>The User Object attribute value in the expected format.</returns>
    public static string AttributeValueFrom(double number)
    {
        return number.ToString("g", CultureInfo.InvariantCulture); // format "g" allows scientific notation as well
    }

    /// <summary>
    /// Converts the specified <see cref="string"/> items to the format expected by array comparison operators (ARRAY CONTAINS ANY OF/ARRAY NOT CONTAINS ANY OF).
    /// </summary>
    /// <param name="items">The <see cref="string"/> items to convert.</param>
    /// <returns>The User Object attribute value in the expected format.</returns>
    public static string AttributeValueFrom(params string[] items)
    {
        return AttributeValueFrom(items.AsEnumerable());
    }

    /// <summary>
    /// Converts the specified <see cref="string"/> items to the format expected by array comparison operators (ARRAY CONTAINS ANY OF/ARRAY NOT CONTAINS ANY OF).
    /// </summary>
    /// <param name="items">The <see cref="string"/> items to convert.</param>
    /// <returns>The User Object attribute value in the expected format.</returns>
    public static string AttributeValueFrom(IEnumerable<string> items)
    {
        return (items ?? throw new ArgumentNullException("items")).Serialize();
    }

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

    private IDictionary<string, string>? custom;

    /// <summary>
    /// Custom attributes of the user for advanced targeting rule definitions (e.g. user role, subscription type, etc.)
    /// </summary>
    public IDictionary<string, string> Custom
    {
        get => this.custom ??= new Dictionary<string, string>();
        set => this.custom = value;
    }

    /// <summary>
    /// Returns all attributes of the user.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetAllAttributes()
    {
        var result = new Dictionary<string, string>();

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
