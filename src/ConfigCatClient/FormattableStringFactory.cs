// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on: https://github.com/dotnet/runtime/blob/v6.0.13/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/FormattableStringFactory.cs
// This is a modified version of the built-in type that is only used only internally in the SDK. It allows
// creating value type instances instead of reference type instances to avoid unnecessary heap allocations.

#pragma warning disable IDE0161 // Convert to file-scoped namespace
#pragma warning disable CS0436 // Type conflicts with imported type

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// A factory type used by compilers to create instances of the type <see cref="FormattableString"/>.
    /// </summary>
    internal static class FormattableStringFactory
    {
        /// <summary>
        /// Create a <see cref="FormattableString"/> from a composite format string and object
        /// array containing zero or more objects to format.
        /// </summary>
        public static FormattableString Create(string format, params object?[] arguments)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            return new FormattableString(format, arguments);
        }
    }
}
