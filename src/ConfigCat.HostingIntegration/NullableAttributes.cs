// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Source: https://github.com/dotnet/runtime/blob/v6.0.5/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs

#pragma warning disable IDE0161 // Convert to file-scoped namespace

namespace System.Diagnostics.CodeAnalysis
{
    // These attributes already shipped with .NET Core 3.1 in System.Runtime
#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
    /// <summary>Specifies that null is disallowed as an input even if the corresponding type allows it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class DisallowNullAttribute : Attribute
    { }
#endif
}
