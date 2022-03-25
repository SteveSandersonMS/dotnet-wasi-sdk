// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Connections;

namespace Wasi.AspNetCore.Server.Native;

internal class WasiConnectionListenerFactory : IConnectionListenerFactory
{
    private bool _hasClaimedPreopenedListener;

    public WasiConnectionListener? BoundConnectionListener { get; private set; }

    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        if (!_hasClaimedPreopenedListener)
        {
            // We'll use the first WASI preopened listener and arbitrarily claim it matches the first requested endpoint.
            // There's no way to know if it actually matches because WASI doesn't provide any address info.
            // If we wanted, we could attach to all the preopened listeners, but it would be very unusual to have more than one.
            _hasClaimedPreopenedListener = true;
            BoundConnectionListener = new WasiConnectionListener(endpoint);
            return ValueTask.FromResult<IConnectionListener>(BoundConnectionListener);
        }
        else
        {
            // For subsequent endpoints, act as if we're listening but no traffic arrives
            return ValueTask.FromResult<IConnectionListener>(new NullConnectionListener(endpoint));
        }
    }

    class NullConnectionListener : IConnectionListener
    {
        public NullConnectionListener(EndPoint endpoint)
        {
            EndPoint = endpoint;
        }

        public EndPoint EndPoint { get; }

        public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
        {
            // Never completes
            return await new TaskCompletionSource<ConnectionContext?>(cancellationToken).Task;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
