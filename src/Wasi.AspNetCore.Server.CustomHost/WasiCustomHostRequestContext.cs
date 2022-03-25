// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Wasi.AspNetCore.Server.CustomHost;

internal class WasiCustomHostRequestContext : WasiServerRequestContext
{
    public uint RequestId { get; }

    public WasiCustomHostRequestContext(uint requestId, string httpMethod, string url, IHeaderDictionary headers, Stream requestBody)
        : base(httpMethod, url, headers, requestBody)
    {
        RequestId = requestId;
    }

    protected override Task TransmitResponseAsync()
    {
        var response = (IHttpResponseFeature)this;

        foreach (var h in response.Headers)
        {
            Interop.ResponseAddHeader(RequestId, h.Key, h.Value.ToString());
        }

        TransmitResponseBody();

        Interop.ResponseComplete(RequestId, response.StatusCode);
        return Task.CompletedTask;
    }

    private unsafe void TransmitResponseBody()
    {
        // TODO: Support non-buffered responses
        Span<byte> chunk = stackalloc byte[4096];
        var responseBody = (IHttpResponseBodyFeature)this;
        fixed (byte* rawResponseBytesPtr = chunk)
        {
            responseBody.Stream.Position = 0;
            while (true)
            {
                var bytesRead = responseBody.Stream.Read(chunk);
                if (bytesRead == 0)
                {
                    break;
                }
                else
                {
                    Interop.ResponseSendChunk(RequestId, rawResponseBytesPtr, bytesRead);
                }
            }
        }
    }
}
