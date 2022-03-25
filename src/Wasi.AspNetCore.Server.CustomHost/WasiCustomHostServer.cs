// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Wasi.AspNetCore.Server.CustomHost;

internal class WasiCustomHostServer : WasiServer
{
    public WasiCustomHostServer(IHostApplicationLifetime lifetime) : base(lifetime)
    {
    }

    protected override void Run<TContext>(IHttpApplication<TContext> application, int port)
    {
        var hostApiServerInterop = new Interop();
        hostApiServerInterop.OnIncomingRequest += (sender, requestArgs) =>
        {
            var requestContext = new WasiCustomHostRequestContext(
                requestArgs.RequestId,
                requestArgs.Method,
                requestArgs.Url,
                ParseHeaders(requestArgs.HeadersCombined),
                requestArgs.Body is null ? new MemoryStream() : new MemoryStream(requestArgs.Body));

            // This isn't meant to throw as it handles its own exceptions and sends errors to the response
            _ = HandleRequestAsync(application, requestContext);
        };

        // The underlying native implementation blocks here. If the listening loop was implemented outside the WASM runtime
        // then we wouldn't block and would just react to subsequent calls into WASM to handle incoming requests.
        Interop.RunHttpServer(hostApiServerInterop, port);
    }

    private static IHeaderDictionary ParseHeaders(string headersCombined)
    {
        // It's not great that we're parsing, stringifying, and then reparsing the HTTP headers
        // More intricate interop could avoid this
        var result = new HeaderDictionary();
        foreach (var headerLine in headersCombined.Split('\n'))
        {
            var colonPos = headerLine.IndexOf(':');
            if (colonPos > 0)
            {
                result.Add(headerLine.Substring(0, colonPos), headerLine.Substring(colonPos + 1));
            }
        }

        return result;
    }
}
