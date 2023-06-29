using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConfigCat.Client.Utils;

internal class IndentedTextBuilder
{
    private readonly StringBuilder stringBuilder = new();
    private int indentLevel;

    public IndentedTextBuilder IncreaseIndent()
    {
        this.indentLevel++;
        return this;
    }

    public IndentedTextBuilder DecreaseIndent()
    {
        Debug.Assert(this.indentLevel > 0, "Evaluate log indentation got invalid.");
        this.indentLevel--;
        return this;
    }

    public virtual IndentedTextBuilder NewLine()
    {
        this.stringBuilder.AppendLine().Insert(this.stringBuilder.Length, "  ", count: this.indentLevel);
        return this;
    }

    public IndentedTextBuilder NewLine(string message)
    {
        return NewLine().Append(message);
    }

    public virtual IndentedTextBuilder Append(object value)
    {
        this.stringBuilder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
        return this;
    }

#if !NET6_0_OR_GREATER
    public virtual IndentedTextBuilder Append(FormattableString value)
    {
        this.stringBuilder.AppendFormat(CultureInfo.InvariantCulture, value.Format, value.GetArguments());
        return this;
    }
#else
    public IndentedTextBuilder Append([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler _)
    {
        // NOTE: The actual work is done by AppendInterpolatedStringHandler.
        return this;
    }

    // Using this wrapper struct we can benefit from .NET 6's performance improvements to interpolated strings
    // (see also https://blog.jetbrains.com/dotnet/2022/02/07/improvements-and-optimizations-for-interpolated-strings-a-look-at-new-language-features-in-csharp-10/).
    [InterpolatedStringHandler]
    public ref struct AppendInterpolatedStringHandler
    {
        private StringBuilder.AppendInterpolatedStringHandler handler;

        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, IndentedTextBuilder logBuilder)
        {
            this.handler = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount, logBuilder.stringBuilder, CultureInfo.InvariantCulture);
        }

        public void AppendLiteral(string value) => this.handler.AppendLiteral(value);

        public void AppendFormatted<T>(T value) => this.handler.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => this.handler.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => this.handler.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => this.handler.AppendFormatted(value, alignment, format);

        public void AppendFormatted(ReadOnlySpan<char> value) => this.handler.AppendFormatted(value);
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => this.handler.AppendFormatted(value, alignment, format);

        public void AppendFormatted(string? value) => this.handler.AppendFormatted(value);
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) => this.handler.AppendFormatted(value, alignment, format);

        public void AppendFormatted(object? value, int alignment = 0, string? format = null) => this.handler.AppendFormatted(value, alignment, format);

        public void AppendFormatted(StringListFormatter value) => value.AppendWith(this.handler);
    }
#endif

    public override string ToString()
    {
        return this.stringBuilder.ToString();
    }
}
