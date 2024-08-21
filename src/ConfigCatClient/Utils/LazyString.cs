using System.Diagnostics;
using System.Globalization;

namespace ConfigCat.Client.Utils;

/// <summary>
/// Defers string formatting until the formatted value is actually needed.
/// </summary>
/// <remarks>
/// It roughly achieves what <c>new Lazy&lt;string&gt;(() => string.Format(CultureInfo.InvariantCulture, format, args), isThreadSafe: false)</c> does
/// but without extra heap memory allocations.
/// </remarks>
internal struct LazyString
{
    private readonly string? format;
    private object? argsOrValue;

    public LazyString(string? value)
    {
        this.format = null;
        this.argsOrValue = value;
    }

    public LazyString(string format, params object?[]? args)
    {
        this.format = format;
        this.argsOrValue = args ?? ArrayUtils.EmptyArray<string>();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? Value
    {
        get
        {
            var argsOrValue = this.argsOrValue;
            if (argsOrValue is null)
            {
                return null;
            }

            if (argsOrValue is string value)
            {
                return value;
            }

            this.argsOrValue = value = string.Format(CultureInfo.InvariantCulture, this.format!, (object?[])argsOrValue);
            return value;
        }
    }

    public bool IsValueCreated => this.argsOrValue is null or string;

    public override string ToString() => Value ?? string.Empty;

    public static implicit operator LazyString(string? value) => new LazyString(value);
}
