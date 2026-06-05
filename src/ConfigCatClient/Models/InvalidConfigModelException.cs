using System;

namespace ConfigCat.Client.Models;

internal sealed class InvalidConfigModelException : InvalidOperationException
{
    public InvalidConfigModelException(string message) : base(message) { }
}
