// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on: https://github.com/dotnet/runtime/blob/v6.0.13/src/libraries/System.Private.CoreLib/src/System/FormattableString.cs
// This is a modified version of the built-in type that is used only internally in the SDK.

#pragma warning disable IDE0161 // Convert to file-scoped namespace
#pragma warning disable CS0436 // Type conflicts with imported type

global using ValueFormattableString = System.FormattableString;

namespace System
{
    internal readonly struct FormattableString : IFormattable
    {
        private readonly string format;
        private readonly object?[] arguments;

        internal FormattableString(string format, object?[] arguments)
        {
            this.format = format;
            this.arguments = arguments;
        }

        public string Format => this.format ?? string.Empty;
        public object?[] GetArguments() { return this.arguments; }

        public string ToString(string? format, IFormatProvider? formatProvider) { return ToString(formatProvider); }
        public string ToString(IFormatProvider? formatProvider) { return string.Format(formatProvider, Format, this.arguments); }
        public override string ToString() { return ToString(Globalization.CultureInfo.CurrentCulture); }
    }
}
