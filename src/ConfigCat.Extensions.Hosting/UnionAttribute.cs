// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Source: https://github.com/dotnet/runtime/blob/v11.0.0-preview.4.26230.115/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/UnionAttribute.cs

#pragma warning disable IDE0161 // Convert to file-scoped namespace

#if !NET11_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that a class or struct is a union type, enabling compiler support for union behaviors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Any class or struct annotated with this attribute is recognized by the C# compiler as a union type.
    /// Union types may support behaviors such as implicit conversions from case types, pattern matching
    /// that unwraps the union's contents, and switch exhaustiveness checking.
    /// </para>
    /// </remarks>
    /// <seealso cref="IUnion" />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class UnionAttribute : Attribute
    {
    }
}

#endif
