// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Wasi.AspNetCore.BundledFiles;

namespace Microsoft.AspNetCore.Builder;

public static class WasiNativeWebApplicationExtensions
{
    public static IApplicationBuilder UseBundledStaticFiles(this IApplicationBuilder app, StaticFileOptions? options = null)
    {
        // Not sure why you'd pass a fileprovider if you're asking to use the bundled files, but just in case,
        // only use the WasiBundledFileProvider if no other was specified
        if (options?.FileProvider is null)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            env.WebRootFileProvider = new WasiBundledFileProvider();
        }

        if (options is null)
        {
            app.UseStaticFiles();
        }
        else
        {
            app.UseStaticFiles(options);
        }

        return app;
    }
}
