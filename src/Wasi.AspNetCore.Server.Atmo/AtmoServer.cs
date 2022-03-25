// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using static Wasi.AspNetCore.Server.Atmo.Interop;

namespace Wasi.AspNetCore.Server.Atmo;

internal class AtmoServer : WasiServer
{
    // Unfortunately Atmo's ABI doesn't provide a mechanism to enumerate all request headers, so we have to
    // ask it for a specific set of well-known headers that may or may not be present
    private static readonly string[] HeaderNames = new[] { "Content-Type", "Accept" };

    public AtmoServer(IHostApplicationLifetime lifetime) : base(lifetime)
    {
    }

    protected override void Run<TContext>(IHttpApplication<TContext> application, int port)
    {
        var hostApiServerInterop = new Interop();
        hostApiServerInterop.OnIncomingRequest += (sender, ident) =>
        {
            var method = Interop.RequestGetField(FieldType.Meta, "method", ident);
            var url = Interop.RequestGetField(FieldType.Meta, "url", ident);
            var headers = GetHeaders(ident);            
            var requestContext = new AtmoRequestContext(
                ident,
                method,
                url,
                headers);

            // This isn't meant to throw as it handles its own exceptions and sends errors to the response
            _ = HandleRequestAsync(application, requestContext);
        };

        // The underlying native implementation blocks here. If the listening loop was implemented outside the WASM runtime
        // then we wouldn't block and would just react to subsequent calls into WASM to handle incoming requests.
        Interop.RunHttpServer(hostApiServerInterop, port);
    }

    private static HeaderDictionary GetHeaders(uint ident)
    {
        var result = new HeaderDictionary();

        for (var headerNameIndex = 0; headerNameIndex < HeaderNames.Length; headerNameIndex++)
        {
            var headerName = HeaderNames[headerNameIndex];
            result[headerName] = Interop.RequestGetField(FieldType.Header, headerName, ident);
        }

        return result;
    }
}
