// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Wasi.AspNetCore.Server.Native;

internal class Interop
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void RunTcpListenerLoop(Interop self);

    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern unsafe void SendResponseData(uint fileDescriptor, byte* buf, int buf_len);

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

    public void RunTcpListenerLoop(Action<WasiConnectionContext> onConnection)
    {
        _onConnectionHandler = onConnection;
        RunTcpListenerLoop(this);
    }

    public void StopTcpListenerLoop()
    {
        throw new NotImplementedException("TODO: Somehow signal the shutdown to the C loop.");
    }
}
