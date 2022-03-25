// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.IO.Pipelines;

namespace Wasi.AspNetCore.Server;

public abstract class WasiServerRequestContext : IHttpRequestFeature, IHttpResponseFeature, IHttpResponseBodyFeature
{
    private List<(Func<object, Task>, object)> _onStartingCallbacks = new();
    private List<(Func<object, Task>, object)> _onCompletedCallbacks = new();

    public WasiServerRequestContext(string httpMethod, string url, IHeaderDictionary headers, Stream requestBody)
    {
        var queryStartPos = url.IndexOf('?');
        var path = queryStartPos < 0 ? url : url.Substring(0, queryStartPos);
        var query = queryStartPos < 0 ? string.Empty : url.Substring(queryStartPos);

        Method = httpMethod;
        Path = path;
        QueryString = query;
        ((IHttpRequestFeature)this).Headers = headers;
        ((IHttpRequestFeature)this).Body = requestBody;
        Stream = new MemoryStream();
        Writer = PipeWriter.Create(Stream);
    }

    public string Protocol { get; set; } = "HTTP/1.1";
    public string Scheme { get; set; } = "http";
    public string Method { get; set; } = "GET";
    public string PathBase { get; set; } = string.Empty;
    public string Path { get; set; } = "/";
    public string QueryString { get; set; } = string.Empty;
    public string RawTarget { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public int StatusCode { get; set; } = 200;
    public string? ReasonPhrase { get; set; }

    public bool HasStarted { get; }

    public Stream Stream { get; }

    public PipeWriter Writer { get; }
    IHeaderDictionary IHttpRequestFeature.Headers { get; set; } = new HeaderDictionary();
    IHeaderDictionary IHttpResponseFeature.Headers { get; set; } = new HeaderDictionary();

    Stream IHttpRequestFeature.Body { get; set; } = default!;
    Stream IHttpResponseFeature.Body { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    protected abstract Task TransmitResponseAsync();

    public async Task CompleteAsync()
    {
        await TransmitResponseAsync();

        foreach (var c in _onCompletedCallbacks)
        {
            await c.Item1(c.Item2);
        }
    }

    public void DisableBuffering()
    {
        throw new NotImplementedException();
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
        _onCompletedCallbacks.Add((callback, state));
    }

    public void OnStarting(Func<object, Task> callback, object state)
    {
        _onStartingCallbacks.Add((callback, state));
    }

    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        using var file = File.OpenRead(path);
        file.CopyTo(Stream); // TODO: Respect offset/count
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        foreach (var c in _onStartingCallbacks)
        {
            await c.Item1(c.Item2);
        }
    }
}
