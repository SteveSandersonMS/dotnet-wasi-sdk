// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Runtime.CompilerServices;

namespace Wasi.AspNetCore.BundledFiles;

public class WasiBundledFileProvider : IFileProvider
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe byte* GetEmbeddedFile(string name, out int length);

    private readonly static DateTime FakeLastModified = new DateTime(2000, 1, 1);

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        throw new NotImplementedException();
    }

    public unsafe IFileInfo GetFileInfo(string subpath)
    {
        var subpathWithoutLeadingSlash = subpath.AsSpan(1);
        var fileBytes = GetEmbeddedFile($"wwwroot/{subpathWithoutLeadingSlash}", out var length);
        return fileBytes == null
            ? new NotFoundFileInfo(subpath)
            : new BundledFileInfo(subpath, length, FakeLastModified, fileBytes);
    }

    public IChangeToken Watch(string filter)
    {
        throw new NotImplementedException();
    }

    unsafe class BundledFileInfo : IFileInfo
    {
        private byte* _fileBytes;

        public BundledFileInfo(string name, long length, DateTime lastModified, byte* fileBytes)
        {
            Name = name;
            LastModified = lastModified;
            Length = length;
            _fileBytes = fileBytes;
        }

        public bool Exists => true;

        public bool IsDirectory => false;

        public DateTimeOffset LastModified { get; }

        public long Length { get; }

        public string Name { get; }

        public string? PhysicalPath => null;

        public Stream CreateReadStream()
            => new UnmanagedMemoryStream(_fileBytes, Length);
    }
}
