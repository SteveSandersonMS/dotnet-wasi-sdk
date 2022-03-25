// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Wasi.AspNetCore.Server.Native;

internal class WasiNativeServer : IServer
{
    private readonly IServer _underlyingServer;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceProvider _services;
    private readonly Interop _interop = new();

    public WasiNativeServer(IServiceProvider services, IServer underlyingServer)
    {
        _services = services;
        _underlyingServer = underlyingServer;
        _lifetime = services.GetRequiredService<IHostApplicationLifetime>();

        // Disable the heartbeat because it tries to run on a background thread
        // Instead we'll manually call the heartbeat at the start of each request (which has drawbacks, but is necessary for now)
        var serviceContext = underlyingServer.GetType().GetProperty("ServiceContext", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(underlyingServer)!;
        var heartbeatProperty = serviceContext.GetType().GetProperty("Heartbeat")!;
        var heartbeat = heartbeatProperty.GetValue(serviceContext)!;
        var onHeartbeatMethod = heartbeat.GetType().GetMethod("OnHeartbeat", BindingFlags.Instance | BindingFlags.NonPublic)!;
        heartbeatProperty.SetValue(serviceContext, null);
        onHeartbeatMethod.Invoke(heartbeat, null);
    }

    public IFeatureCollection Features => _underlyingServer.Features;

    public void Dispose() => _underlyingServer.Dispose();

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        var result = _underlyingServer.StartAsync(application, cancellationToken);
        _lifetime.ApplicationStarted.Register(() => Run(application));
        return result;
    }

    private void Run<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        if (_services.GetRequiredService<IConnectionListenerFactory>() is not WasiConnectionListenerFactory factory)
        {
            throw new InvalidOperationException($"{typeof(WasiNativeServer)} requires the {typeof(IConnectionListenerFactory)} to be an instance of {typeof(WasiConnectionListenerFactory)}");
        }

        if (factory.BoundConnectionListener is not { } listener)
        {
            throw new InvalidOperationException("The WASI connection listener factory was not bound to any endpoint.");
        }

        _interop.RunTcpListenerLoop(onConnection: listener.ReceiveConnection);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _interop.StopTcpListenerLoop();
        return _underlyingServer.StopAsync(cancellationToken);
    }
}
