using System;
using System.Globalization;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Represents a plain log message or a log message format with named arguments.
/// </summary>
public struct FormattableLogMessage : IFormattable
{
    private static string ToFormatString(string message)
    {
        return message.Replace("{", "{{").Replace("}", "}}");
    }

    internal static FormattableLogMessage FromInterpolated(ValueFormattableString message, string[] argNames)
    {
        return new FormattableLogMessage(message.Format,
            argNames ?? Array.Empty<string>(),
            message.GetArguments() ?? Array.Empty<object?>());
    }

    private readonly string? format;
    private readonly string[]? argNames;
    private readonly object?[]? argValues;
    private string? invariantFormattedMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattableLogMessage"/> struct from a plain log message.
    /// </summary>
    public FormattableLogMessage(string message)
    {
        this.invariantFormattedMessage = message ?? throw new ArgumentNullException(nameof(message));
        this.format = null;
        this.argNames = null;
        this.argValues = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattableLogMessage"/> struct from a log message format and the corresponding named arguments.
    /// </summary>
    public FormattableLogMessage(string format, string[] argNames, object?[] argValues)
    {
        this.format = format ?? throw new ArgumentNullException(nameof(format));
        this.argNames = argNames ?? throw new ArgumentNullException(nameof(argNames));
        this.argValues = argValues ?? throw new ArgumentNullException(nameof(argValues));
        if (argNames.Length != argValues.Length)
        {
            throw new ArgumentException($"Number of argument names ({argNames.Length}) and argument values ({argValues.Length}) mismatch.", nameof(argNames));
        }
        this.invariantFormattedMessage = null;
    }

    /// <summary>
    /// Log message format.
    /// </summary>
    public readonly string Format => this.format ?? ToFormatString(this.invariantFormattedMessage ?? string.Empty);

    /// <summary>
    /// Names of the named arguments.
    /// </summary>
    public readonly string[] ArgNames => this.argNames ?? Array.Empty<string>();

    /// <summary>
    /// Values of the named arguments.
    /// </summary>
    public readonly object?[] ArgValues => this.argValues ?? Array.Empty<object?>();

    /// <summary>
    /// The log message formatted using <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    public string InvariantFormattedMessage => this.invariantFormattedMessage ??= ToString(CultureInfo.InvariantCulture);

    internal LazyString ToLazyString()
    {
        return this.invariantFormattedMessage
            ?? new LazyString(this.format ?? string.Empty, this.argValues);
    }

    /// <summary>
    /// Returns the log message formatted using <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    public override readonly string ToString()
    {
        return ToString(formatProvider: null);
    }

    /// <summary>
    /// Returns the log message formatted using the specified <paramref name="formatProvider"/>.
    /// </summary>
    public readonly string ToString(IFormatProvider? formatProvider)
    {
        return this.format is not null
            ? string.Format(formatProvider, this.format, ArgValues)
            : (this.invariantFormattedMessage ?? string.Empty);
    }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString(formatProvider);
    }
}
