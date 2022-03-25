// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using System.Net;
using System.Threading.Channels;

namespace Wasi.AspNetCore.Server.Native;

internal class WasiConnectionListener : IConnectionListener
{
    private Channel<ConnectionContext?> _arrivingConnections = Channel.CreateUnbounded<ConnectionContext?>();

    public WasiConnectionListener(EndPoint endpoint)
    {
        EndPoint = endpoint;
    }

    public EndPoint EndPoint { get; }

    public ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        return _arrivingConnections.Reader.ReadAsync(cancellationToken);
    }

    public void ReceiveConnection(ConnectionContext connectionContext)
    {
        if (!_arrivingConnections.Writer.TryWrite(connectionContext))
        {
            throw new InvalidOperationException("Unable to add the incoming connection to the channel");
        }
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask; // Not applicable - preopened listeners can't be closed within the app's lifetime
}
