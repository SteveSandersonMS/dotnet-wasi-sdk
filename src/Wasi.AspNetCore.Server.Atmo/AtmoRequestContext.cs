// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using static Wasi.AspNetCore.Server.Atmo.AtmoLogger;

namespace Wasi.AspNetCore.Server.Atmo;

internal class AtmoRequestContext : WasiServerRequestContext
{
    public uint Ident { get; }

    public AtmoRequestContext(uint ident, string httpMethod, string url, HeaderDictionary headers)
        : base(httpMethod, url, headers, new MemoryStream())
    {
        Ident = ident;
    }

    protected override unsafe Task TransmitResponseAsync()
    {
        AtmoLogsWriterStream.EmitPendingMessages(Ident);

        var response = (IHttpResponseFeature)this;

        foreach (var h in response.Headers)
        {
            Interop.ResponseAddHeader(Ident, h.Key, h.Value.ToString());
        }

        var ms = new MemoryStream();

        var responseBody = (IHttpResponseBodyFeature)this;
        responseBody.Stream.Position = 0;
        responseBody.Stream.CopyTo(ms);
        var responseBodyBuffer = ms.GetBuffer();

        fixed (byte* rawResponseBytesPtr = responseBodyBuffer)
        {
            Interop.ResponseComplete(Ident, response.StatusCode, rawResponseBytesPtr, (int)ms.Length);
        }
            
        return Task.CompletedTask;
    }
}
