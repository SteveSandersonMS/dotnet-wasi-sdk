// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wasi.AspNetCore.Server.Native;

namespace Microsoft.AspNetCore.Builder;

public static class WasiNativeWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder UseWasiConnectionListener(this WebApplicationBuilder builder)
    {
        // We want the IServer to be the usual KestrelServerImpl, but we also want to replace its
        // StartAsync with our own version that calls the native blocking listener loop. So, get
        // the usual instance and pass it through to the WASI-specific wrapper.
        var underlyingServerType = FindUnderlyingServerType(builder);
        builder.Services.AddSingleton(underlyingServerType);
        builder.Services.AddSingleton<IServer>(serviceProvider =>
        {
            var underlying = serviceProvider.GetRequiredService(underlyingServerType);
            return new WasiNativeServer(serviceProvider, (IServer)underlying);
        });

        builder.Services.AddSingleton<IConnectionListenerFactory, WasiConnectionListenerFactory>();
        builder.Logging.AddProvider(new WasiLoggingProvider()).AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Error);
        
        return builder;
    }

    private static Type FindUnderlyingServerType(WebApplicationBuilder builder)
    {
        foreach (var s in builder.Services)
        {
            if (s.ServiceType == typeof(IServer) && s.ImplementationType is Type type)
            {
                return type;
            }
        }

        throw new InvalidOperationException("The service collection doesn't contain an existing IServer implementation type.");
    }
}
