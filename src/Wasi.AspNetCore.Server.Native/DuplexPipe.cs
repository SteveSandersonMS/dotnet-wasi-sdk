// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Wasi.AspNetCore.Server.Native;

internal class DuplexPipe : IDuplexPipe
{
    public DuplexPipe(PipeReader reader, PipeWriter writer)
    {
        Input = reader;
        Output = writer;
    }

    public PipeReader Input { get; }

    public PipeWriter Output { get; }

    public static (DuplexPipe ApplicationToTransport, DuplexPipe TransportToApplication) CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
    {
        var input = new Pipe(inputOptions);   // Input from the server's perspective
        var output = new Pipe(outputOptions); // Output from the server's perspective

        var transportToApplication = new DuplexPipe(output.Reader, input.Writer); // API for writing server input and reading server output
        var applicationToTransport = new DuplexPipe(input.Reader, output.Writer); // API for reading server input and writing server output 

        return (applicationToTransport, transportToApplication);
    }
}
