// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Wasi.AspNetCore.Server.CustomHost;

public static class CustomHostWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder UseWasiCustomHostServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IServer, WasiCustomHostServer>();
        builder.Logging.AddProvider(new WasiLoggingProvider()).AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Error);
        return builder;
    }
}
