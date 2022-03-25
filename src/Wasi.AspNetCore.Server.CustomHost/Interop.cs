// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Wasi.AspNetCore.Server.CustomHost;

internal class Interop
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void RunHttpServer(Interop owner, int port);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void ResponseAddHeader(uint requestId, string name, string value);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void ResponseSendChunk(uint requestId, byte* buffer, int buffer_length);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void ResponseComplete(uint requestId, int statusCode);

    public event EventHandler<(uint RequestId, string Method, string Url, string HeadersCombined, byte[]? Body)>? OnIncomingRequest;

    // TODO: Make sure this doesn't get trimmed if AOT compiled
    // The requestId is a uint instead of a long because otherwise the runtime fails with an error like "CANNOT HANDLE INTERP ICALL SIG VLI"
    // For a more complete implementation you might want to use a GUID string instead.
    private unsafe void HandleIncomingRequest(uint requestId, string method, string url, string headersCombined, byte[]? body)
        => OnIncomingRequest?.Invoke(this, (requestId, method, url, headersCombined, body));
}
