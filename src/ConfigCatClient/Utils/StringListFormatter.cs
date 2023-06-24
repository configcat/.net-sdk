using System.Collections.Generic;

namespace ConfigCat.Client.Utils;

internal readonly struct StringListFormatter
{
    private readonly ICollection<string> collection;

    public StringListFormatter(ICollection<string> collection)
    {
        this.collection = collection;
    }

    public override string ToString()
    {
        if (this.collection is { Count: > 0 })
        {
            return "'" + string.Join("', '", this.collection) + "'";
        }

        return string.Empty;
    }
}
