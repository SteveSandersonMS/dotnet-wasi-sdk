using System.Collections.Concurrent;

namespace Wasi.AspNetCore.Server.Atmo;

public static class AtmoLogger
{
    public static void RedirectConsoleToAtmoLogs()
    {
        Console.SetError(new StreamWriter(new AtmoLogsWriterStream()) { AutoFlush = true });
        Console.SetOut(new StreamWriter(new AtmoLogsWriterStream()) { AutoFlush = true });
    }

    internal class AtmoLogsWriterStream : Stream
    {
        private static ConcurrentQueue<byte[]> _pendingMessages = new();

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotImplementedException();

        public override void SetLength(long value)
            => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _pendingMessages.Enqueue(new Span<byte>(buffer, offset, count).ToArray());
        }

        internal static unsafe void EmitPendingMessages(uint ident)
        {
            while (_pendingMessages.TryDequeue(out var message))
            {
                fixed (byte* messagePtr = message)
                {
                    Interop.LogMessageRaw(ident, 1, messagePtr, message.Length);
                }
            }
        }
    }
}
