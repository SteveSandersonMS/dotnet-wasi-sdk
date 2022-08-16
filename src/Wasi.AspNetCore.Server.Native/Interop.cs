// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyMetadata("WasmImportModule", "wasiaspnetcoreservernative")]

// Haven't fully tracked down why, but this is required for the pinvokes to work
// Maybe it wouldn't be required if I was using all of the updated ManagedToNative codegen from the runtime repo
[assembly: DisableRuntimeMarshalling]

namespace Wasi.AspNetCore.Server.Native;

internal class Interop
{
    [DllImport("wasiaspnetcoreservernative")]
    private static extern unsafe void run_tcp_listener_loop(void* self);

    [DllImport("wasiaspnetcoreservernative")]
    public static extern unsafe void send_response_data(uint fileDescriptor, byte* buf, int buf_len);

    private Action<WasiConnectionContext>? _onConnectionHandler;
    private ConcurrentDictionary<uint, WasiConnectionContext> _liveConnections = new();

    // TODO: Make sure this doesn't get trimmed if AOT compiled
    private void NotifyOpenedConnection(uint fileDescriptor)
    {
        var connectionInfo = new WasiConnectionContext(fileDescriptor);
        _liveConnections[connectionInfo.FileDescriptor] = connectionInfo;
        _onConnectionHandler!(connectionInfo);
    }

    private void NotifyClosedConnection(uint fileDescriptor)
    {
        if (_liveConnections.TryRemove(fileDescriptor, out var closedConnection))
        {
            _ = closedConnection.NotifyClosedByClientAsync();
        }
    }

    private unsafe void NotifyDataReceived(uint fileDescriptor, byte* buf, int buf_len)
    {
        if (_liveConnections.TryGetValue(fileDescriptor, out var connection))
        {
            var data = new ReadOnlySpan<byte>(buf, buf_len);
            connection.ReceiveDataFromClient(data);
        }
    }

    public unsafe void RunTcpListenerLoop(Action<WasiConnectionContext> onConnection)
    {
        _onConnectionHandler = onConnection;
        var self = this;
        var selfPtr = Unsafe.AsPointer(ref self);
        run_tcp_listener_loop(selfPtr);
    }

    public void StopTcpListenerLoop()
    {
        throw new NotImplementedException("TODO: Somehow signal the shutdown to the C loop.");
    }
}
