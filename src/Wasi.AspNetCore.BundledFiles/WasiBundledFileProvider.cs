// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[assembly: AssemblyMetadata("WasmImportModule", "aspnetcorebundledfiles")]
[assembly: DisableRuntimeMarshalling]

namespace Wasi.AspNetCore.BundledFiles;

public class WasiBundledFileProvider : IFileProvider
{
    [DllImport("aspnetcorebundledfiles")]
    private static extern unsafe byte* mono_get_embedded_file(void* name, int* length);

    private readonly static DateTime FakeLastModified = new DateTime(2000, 1, 1);

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return new BundledDirectoryContents(this, subpath);
    }

    public unsafe IFileInfo GetFileInfo(string subpath)
    {
        var subpathWithoutLeadingSlash = subpath.AsSpan(1);
        int length;
        var name = $"wwwroot/{subpathWithoutLeadingSlash}";
        var fileBytes = mono_get_embedded_file(Unsafe.AsPointer(ref name), &length);
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

    class BundledDirectoryContents : IDirectoryContents
    {
        private readonly IFileProvider _owner;
        private readonly string _subpath;

        public BundledDirectoryContents(IFileProvider owner, string subpath)
        {
            _owner = owner;
            _subpath = subpath;
        }

        public bool Exists => _subpath == "/";

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            // TODO: Mechanism for enumerating everything in a bundled directory
            // Currently this only recognizes index.html files to support UseDefaultFiles
            if (_subpath == "/")
            {
                var fileInfo = _owner.GetFileInfo("/index.html");
                if (fileInfo.Exists)
                {
                    yield return fileInfo;
                }
            }
        }
    }
}
