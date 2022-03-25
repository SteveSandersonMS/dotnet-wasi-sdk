// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wasi.AspNetCore.Server.Atmo.Services;

namespace Wasi.AspNetCore.Server.Atmo;

public static class AtmoWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder UseAtmoServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IServer, AtmoServer>();
        builder.Services.AddScoped<IdentAccessor>();
        builder.Services.AddHttpContextAccessor();
        builder.Logging.AddProvider(new WasiLoggingProvider());
        return builder;
    }

    public static void AddAtmoCache(this IServiceCollection services)
    {
        services.AddScoped<Services.Cache>(servicesProvider =>
        {
            var contextAccessor = servicesProvider.GetRequiredService<IHttpContextAccessor>();
            var context = contextAccessor.HttpContext!;
            var request = (AtmoRequestContext)context.Features.Get<IHttpRequestFeature>()!;
            return new Services.Cache(request.Ident);
        });
    }
}
