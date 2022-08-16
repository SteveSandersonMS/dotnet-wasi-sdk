// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using System.Buffers;
using System.IO.Pipelines;

namespace Wasi.AspNetCore.Server.Native;

internal class WasiConnectionContext : ConnectionContext
{
    // TODO: Consider configuring some equivalent to the PinnedBlockMemoryPool
    private static readonly PipeOptions TransportPipeOptions = new PipeOptions(readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);

    private readonly IDuplexPipe _applicationToTransport;
    private readonly IDuplexPipe _transportToApplication;
    private readonly string _connectionId;
    private readonly CancellationTokenSource _connectionClosedCts = new();

    public WasiConnectionContext(uint fileDescriptor)
    {
        FileDescriptor = fileDescriptor;
        _connectionId = $"Connection_{fileDescriptor}";
        (_applicationToTransport, _transportToApplication) = DuplexPipe.CreateConnectionPair(TransportPipeOptions, TransportPipeOptions);

        // This shouldn't throw, or at least it needs to do its own error handling
        _ = TransmitOutputToClientAsync(_connectionClosedCts.Token);
    }

    public uint FileDescriptor { get; }
    public override IDuplexPipe Transport { get => _applicationToTransport; set => throw new NotImplementedException(); }
    public override string ConnectionId { get => _connectionId; set => throw new NotImplementedException(); }
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();

    public void ReceiveDataFromClient(ReadOnlySpan<byte> data)
    {
        _transportToApplication.Output.WriteAsync(data.ToArray()); // TODO: Not this
    }

    public async Task NotifyClosedByClientAsync()
    {
        // TODO: Is this enough to make ASP.NET Core know the client is gone?
        await _transportToApplication.Output.FlushAsync();
        await _transportToApplication.Output.CompleteAsync();
        _connectionClosedCts.Cancel();
    }

    private async Task TransmitOutputToClientAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var readResult = await _transportToApplication.Input.ReadAsync(cancellationToken);
                if (readResult.Buffer.Length > 0)
                {
                    SendBufferAsResponse(readResult.Buffer);
                    _transportToApplication.Input.AdvanceTo(readResult.Buffer.End);
                }

                if (readResult.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // This is fine - it's ReadAsync being cancelled due to the connection being closed
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private unsafe void SendBufferAsResponse(ReadOnlySequence<byte> buffer)
    {
        foreach (var chunk in buffer)
        {
            fixed (byte* bufPtr = chunk.Span)
            {
                Interop.send_response_data(FileDescriptor, bufPtr, chunk.Length);
            }
        }
    }
}
