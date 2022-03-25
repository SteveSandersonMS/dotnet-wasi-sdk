// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.StaticFiles;
using Wasi.AspNetCore.BundledFiles;

namespace Microsoft.AspNetCore.Builder;

public static class WasiNativeWebApplicationExtensions
{
    public static IApplicationBuilder UseBundledStaticFiles(this IApplicationBuilder app, StaticFileOptions? options = null)
    {
        options = options ?? new StaticFileOptions()
        {
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
            ServeUnknownFileTypes = true
        };

        if (options.FileProvider is not null)
        {
            throw new ArgumentException("The options must not specify a file provider.");
        }

        options.FileProvider = new WasiBundledFileProvider();

        app.UseStaticFiles(options);

        return app;
    }
}
