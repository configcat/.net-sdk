using System.Globalization;

namespace ConfigCat.Client.Utils;

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

    public string? Value => this.argsOrValue as string;

    public override string? ToString()
    {
        var argsOrValue = this.argsOrValue;
        if (argsOrValue is null)
        {
            return null;
        }

        if (argsOrValue is not string value)
        {
            var args = (object?[])argsOrValue;
            this.argsOrValue = value = string.Format(CultureInfo.InvariantCulture, this.format!, args);
        }

        return value;
    }

    public static implicit operator LazyString(string? value) => new LazyString(value);
}
