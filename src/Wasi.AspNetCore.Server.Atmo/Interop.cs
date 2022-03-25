// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using static Wasi.AspNetCore.Server.Atmo.AtmoLogger;

namespace Wasi.AspNetCore.Server.Atmo;

internal class Interop
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void RunHttpServer(Interop owner, int port);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void ResponseAddHeader(uint ident, string name, string value);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void ResponseComplete(uint ident, int statusCode, byte* body, int body_len);

    public event EventHandler<uint>? OnIncomingRequest;

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern void LogMessage(uint ident, int level, string message);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static unsafe extern void LogMessageRaw(uint ident, int level, byte* message, int message_len);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern string RequestGetField(FieldType fieldType, string fieldName, uint ident);

    // TODO: Make sure this doesn't get trimmed if AOT compiled
    // It's static because we want it to be able to emit pending logs even if 'interop' is null
    private static unsafe void HandleIncomingRequest(Interop interop, uint ident)
    {
        AtmoLogsWriterStream.EmitPendingMessages(ident);
        interop.OnIncomingRequest?.Invoke(interop, ident);
    }

    public enum FieldType : int
    {
        Meta = 0,
        Body = 1,
        Header = 2,
        Params = 3,
        State = 4,
    }
}
