using System.Runtime.CompilerServices;
using System.Text;

namespace Wasi.AspNetCore.Server.Atmo.Services;

public class Cache
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    static unsafe extern byte[]? Get(uint ident, string key);

    [MethodImpl(MethodImplOptions.InternalCall)]
    static unsafe extern int Set(uint ident, string key, byte* value, int value_len, int ttl);

    private readonly uint _ident;

    public Cache(uint ident)
    {
        _ident = ident;
    }

    public unsafe byte[]? GetBytes(string key)
    {
        return Get(_ident, key);
    }

    public unsafe string? GetString(string key)
    {
        var bytes = GetBytes(key);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    public unsafe void Set(string key, Span<byte> value, TimeSpan ttl)
    {
        fixed (byte* valuePtr = value)
        {
            Set(_ident, key, valuePtr, value.Length, (int)ttl.TotalSeconds);
        }
    }

    public unsafe void Set(string key, string value, TimeSpan ttl)
    {
        Set(key, Encoding.UTF8.GetBytes(value), ttl);
    }
}
