using System;

namespace ConfigCat.Client;

internal sealed class InvalidConfigModelException : InvalidOperationException
{
    public InvalidConfigModelException(string message) : base(message) { }
}
