// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis;

// These are used to make the shared sources from WasmAppBuilder compile for .NET 4.7 for VS tasks

#if NET7_0_OR_GREATER
#else // .NET Framework
class NotNullAttribute : Attribute { }
class NotNullWhenAttribute : Attribute { public NotNullWhenAttribute(bool value) {} }
#endif
