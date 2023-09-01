using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConfigCat.Client.Utils;

internal readonly struct StringListFormatter : IFormattable
{
    private readonly ICollection<string> collection;
    private readonly int maxLength;
    private readonly Func<int, string>? getOmittedItemsText;

    public StringListFormatter(ICollection<string> collection, int maxLength = 0, Func<int, string>? getOmittedItemsText = null)
    {
        this.collection = collection;
        this.maxLength = maxLength;
        this.getOmittedItemsText = getOmittedItemsText;
    }

    private static string GetSeparator(string? format) => format == "a" ? "' -> '" : "', '";

#if NET6_0_OR_GREATER
    public void AppendWith(ref StringBuilder.AppendInterpolatedStringHandler handler, string? format = null)
    {
        if (this.collection is { Count: > 0 })
        {
            var i = 0;
            var n = this.maxLength > 0 && this.collection.Count > this.maxLength ? this.maxLength : this.collection.Count;
            var separator = GetSeparator(format);
            var currentSeparator = string.Empty;

            handler.AppendLiteral("'");
            foreach (var item in this.collection)
            {
                handler.AppendLiteral(currentSeparator);
                handler.AppendLiteral(item);
                currentSeparator = separator;

                if (++i >= n)
                {
                    break;
                }
            }
            handler.AppendLiteral("'");

            if (this.getOmittedItemsText is not null && n < this.collection.Count)
            {
                handler.AppendLiteral(this.getOmittedItemsText(this.collection.Count - this.maxLength));
            }
        }
    }
#endif

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (this.collection is { Count: > 0 })
        {
            IEnumerable<string> items = this.collection;
            string appendix;

            if (this.maxLength > 0 && this.collection.Count > this.maxLength)
            {
                items = items.Take(this.maxLength);
                appendix = this.getOmittedItemsText?.Invoke(this.collection.Count - this.maxLength) ?? string.Empty;
            }
            else
            {
                appendix = string.Empty;
            }

            return "'" + string.Join(GetSeparator(format), items) + "'" + appendix;
        }

        return string.Empty;
    }

    public override string ToString() => ToString(null, null);
}
