// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace Wasi.AspNetCore.Server;

public abstract class WasiServer : IServer, IServerAddressesFeature
{
    private readonly IHostApplicationLifetime _lifetime;

    public IFeatureCollection Features { get; } = new FeatureCollection();

    ICollection<string> IServerAddressesFeature.Addresses { get; } = new List<string>();

    bool IServerAddressesFeature.PreferHostingUrls { get; set; }

    public WasiServer(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
        Features[typeof(IServerAddressesFeature)] = this;
    }

    protected abstract void Run<TContext>(IHttpApplication<TContext> application, int port) where TContext : notnull;

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        // Where possible we'll use the port given via IServerAddressesFeature (e.g., via ASPNETCORE_URLS env var)
        // but if not fall back on port 5000. We're only binding to one address though.
        var addresses = ((IServerAddressesFeature)this).Addresses;
        if (addresses.Count == 0)
        {
            addresses.Add("http://localhost:5000");
        }
        var port = Uri.TryCreate(addresses.First().Replace("*", "host"), default, out var uri)
            ? uri.Port : 5000;

        _lifetime.ApplicationStarted.Register(() => Run(application, port));

        return Task.CompletedTask;
    }

    protected Task HandleRequestAsync<TContext>(IHttpApplication<TContext> application, WasiServerRequestContext requestContext) where TContext : notnull
    {
        var resultTask = HandleRequestCoreAsync(application, requestContext);
        if (resultTask.IsCompleted)
        {
            // If possible, process it synchronously, as the host doesn't currently support throwing asynchronously
            return CompleteResponseWithErrorHandling();
        }
        else
        {
            var tcs = new TaskCompletionSource();
            resultTask.GetAwaiter().OnCompleted(async () =>
            {
                try
                {
                    await CompleteResponseWithErrorHandling();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }

        Task CompleteResponseWithErrorHandling()
        {
            if (resultTask.IsFaulted)
            {
                requestContext.StatusCode = 500;
                requestContext.Stream.Write(
                    Encoding.UTF8.GetBytes($"<h1>Server error</h1><pre>{resultTask.Exception}</pre>"));
            }

            return requestContext.CompleteAsync();
        }
    }

    private async Task HandleRequestCoreAsync<TContext>(IHttpApplication<TContext> application, WasiServerRequestContext requestContext) where TContext: notnull
    {
        var requestFeatures = new FeatureCollection();
        requestFeatures[typeof(IHttpRequestFeature)] = requestContext;
        requestFeatures[typeof(IHttpResponseFeature)] = requestContext;
        requestFeatures[typeof(IHttpResponseBodyFeature)] = requestContext;

        var ctx = application.CreateContext(requestFeatures);
        try
        {
            await application.ProcessRequestAsync(ctx);
            application.DisposeContext(ctx, null);
        }
        catch (Exception ex)
        {
            application.DisposeContext(ctx, ex);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public void Dispose()
    {
    }
}
