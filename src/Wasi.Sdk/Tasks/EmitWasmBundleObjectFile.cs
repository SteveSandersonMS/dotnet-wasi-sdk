// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace Wasi.Sdk.Tasks;

public class EmitWasmBundleObjectFile : Microsoft.Build.Utilities.Task, ICancelableTask
{
    private static Encoding Utf8NoBom = new UTF8Encoding(false);
    private static byte[] HexToUtf8Lookup;
    private static byte[] NewLineAndIndentation = new[] { (byte)0x0a, (byte)0x20, (byte)0x20 };
    private CancellationTokenSource BuildTaskCancelled { get; } = new();

    [Required]
    public ITaskItem[] FilesToBundle { get; set; }

    [Required]
    public string ClangExecutable { get; set; }

    [Output]
    public string BundleApiSourceCode { get; set; }

    static EmitWasmBundleObjectFile()
    {
        BuildHexToUtf8Lookup();
    }

    private static void BuildHexToUtf8Lookup()
    {
        // Every 6 bytes in this array represents the output for a different input byte value.
        // For example, the input byte 0x1a (26 decimal) corresponds to bytes 156-161 (26*6=156),
        // whose values will be ['0', 'x', '1', 'a', ',', ' '], which is the UTF-8 representation
        // for "0x1a, ". This is just a faster alternative to calling .ToString("x2") on every
        // byte of the input file and then pushing that string through UTF8Encoding.
        HexToUtf8Lookup = new byte[256 * 6];
        for (var i = 0; i < 256; i++)
        {
            string byteAsHex = i.ToString("x2");
            char highOrderChar = BitConverter.IsLittleEndian ? byteAsHex[0] : byteAsHex[1];
            char lowOrderChar = BitConverter.IsLittleEndian ? byteAsHex[1] : byteAsHex[0];
            HexToUtf8Lookup[i * 6 + 0] = (byte)'0';
            HexToUtf8Lookup[i * 6 + 1] = (byte)'x';
            HexToUtf8Lookup[i * 6 + 2] = (byte)highOrderChar;
            HexToUtf8Lookup[i * 6 + 3] = (byte)lowOrderChar;
            HexToUtf8Lookup[i * 6 + 4] = (byte)',';
            HexToUtf8Lookup[i * 6 + 5] = (byte)' ';
        }
    }

    public override bool Execute()
    {
        // We're handling the incrementalism within this task, because it needs to be based on file content hashes
        // and not on timetamps. The output filenames contain a content hash, so if any such file already exists on
        // disk with that name, we know it must be up-to-date.
        var remainingFilesToBundle = FilesToBundle.Where(f => !File.Exists(f.GetMetadata("ObjectFile"))).ToArray();

        // If you're only touching the leaf project, we don't really need to tell you that.
        // But if there's more work to do it's valuable to show progress.
        var verbose = remainingFilesToBundle.Length > 1;
        var verboseCount = 0;

        Parallel.For(0, remainingFilesToBundle.Length, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = BuildTaskCancelled.Token }, i =>
        {
            var item = remainingFilesToBundle[i];
            
            var outputFile = item.GetMetadata("ObjectFile");
            if (verbose)
            {
                var count = Interlocked.Increment(ref verboseCount);
                Log.LogMessage(MessageImportance.High, "{0}/{1} Bundling {2}...", count, remainingFilesToBundle.Length, Path.GetFileName(item.ItemSpec));
            }

            EmitObjectFile(item, outputFile);
        });

        BundleApiSourceCode = GetBundleFileApiSource(FilesToBundle);

        return !Log.HasLoggedErrors;
    }

    private void EmitObjectFile(ITaskItem fileToBundle, string destinationObjectFile)
    {
        Log.LogMessage(MessageImportance.Low, "Bundling {0} as {1}", fileToBundle.ItemSpec, destinationObjectFile);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationObjectFile));

        var clangProcess = Process.Start(new ProcessStartInfo
        {
            FileName = ClangExecutable,
            Arguments = $"-xc -o \"{destinationObjectFile}\" -c -",
            RedirectStandardInput = true,
            UseShellExecute = false,
        });

        BundleFileToCSource(fileToBundle, clangProcess.StandardInput.BaseStream);
        clangProcess.WaitForExit();
    }

    private static string GetBundleFileApiSource(ITaskItem[] bundledFiles)
    {
        // Emit an object file that uses all the bundle file symbols and supplies an API
        // for getting the bundled file data at runtime
        var symbols = bundledFiles.Select(ToSafeSymbolName).ToArray();
        var result = new StringBuilder();

        result.AppendLine("#include <string.h>");
        result.AppendLine();
        result.AppendLine("int mono_wasm_add_assembly(const char* name, const unsigned char* data, unsigned int size);");
        result.AppendLine();

        foreach (var file in bundledFiles)
        {
            var symbol = ToSafeSymbolName(file);
            result.AppendLine($"extern const unsigned char {symbol}[];");
            result.AppendLine($"extern const int {symbol}_len;");
        }

        result.AppendLine();
        result.AppendLine("const unsigned char* dotnet_wasi_getbundledfile(const char* name, int* out_length) {");

        // TODO: Instead of a naive O(N) search through all bundled files, consider putting them in a
        // hashtable or at least generating a series of comparisons equivalent to a binary search

        foreach (var file in bundledFiles.Where(f => !string.Equals(f.GetMetadata("WasmRole"), "assembly", StringComparison.OrdinalIgnoreCase)))
        {
            var symbol = ToSafeSymbolName(file);
            result.AppendLine($"  if (!strcmp (name, \"{file.ItemSpec.Replace("\\", "/")}\")) {{");
            result.AppendLine($"    *out_length = {symbol}_len;");
            result.AppendLine($"    return {symbol};");
            result.AppendLine("  }");
            result.AppendLine();
        }

        result.AppendLine("  return NULL;");
        result.AppendLine("}");

        result.AppendLine();
        result.AppendLine("void dotnet_wasi_registerbundledassemblies() {");

        foreach (var file in bundledFiles.Where(f => string.Equals(f.GetMetadata("WasmRole"), "assembly", StringComparison.OrdinalIgnoreCase)))
        {
            var symbol = ToSafeSymbolName(file);
            result.AppendLine($"  mono_wasm_add_assembly (\"{ Path.GetFileName(file.ItemSpec) }\", {symbol}, {symbol}_len);");
        }

        result.AppendLine("}");

        return result.ToString();
    }

    private static void BundleFileToCSource(ITaskItem fileToBundle, Stream outputStream)
    {
        // Emits a C source file in the same format as "xxd --include". Example:
        //
        // unsigned char Some_File_dll[] = {
        //   0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x0a
        // };
        // unsigned int Some_File_dll_len = 6;

        using var inputStream = File.OpenRead(fileToBundle.ItemSpec);
        using var outputUtf8Writer = new StreamWriter(outputStream, Utf8NoBom);

        var symbolName = ToSafeSymbolName(fileToBundle);
        outputUtf8Writer.Write($"unsigned char {symbolName}[] = {{");
        outputUtf8Writer.Flush();

        var buf = new byte[4096];
        var bytesRead = 0;
        var generatedArrayLength = 0;
        var bytesEmitted = 0;
        while ((bytesRead = inputStream.Read(buf, 0, buf.Length)) > 0)
        {
            for (var i = 0; i < bytesRead; i++)
            {
                if (bytesEmitted++ % 12 == 0)
                {
                    outputStream.Write(NewLineAndIndentation, 0, NewLineAndIndentation.Length);
                }

                var byteValue = buf[i];
                outputStream.Write(HexToUtf8Lookup, byteValue * 6, 6);
            }

            generatedArrayLength += bytesRead;
        }

        outputStream.Flush();
        outputUtf8Writer.WriteLine("0\n};");
        outputUtf8Writer.WriteLine($"unsigned int {symbolName}_len = {generatedArrayLength};");
    }

    private static string ToSafeSymbolName(ITaskItem fileToBundle)
    {
        var name = Path.GetFileName(fileToBundle.ItemSpec);

        // Equivalent to the logic from "xxd --include"
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            sb.Append(IsAlphanumeric(c) ? c : '_');
        }

        // Not equivalent to xxd - append a hash of the full path to ensure uniqueness
        // even if we have both dir1/filename and dir2/filename
        using var hashAlg = SHA256.Create();
        var hashBytes = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(fileToBundle.ItemSpec));
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "");
        sb.Append("_");
        sb.Append(hashString.Substring(0, 8));

        return sb.ToString();
    }

    // Equivalent to "isalnum"
    private static bool IsAlphanumeric(char c) => c
        is (>= 'a' and <= 'z')
        or (>= 'A' and <= 'Z')
        or (>= '0' and <= '9');

    public void Cancel()
    {
        BuildTaskCancelled.Cancel();
    }
}
