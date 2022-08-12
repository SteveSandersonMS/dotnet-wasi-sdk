// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Wasi.Sdk.Tasks;

/// <summary>
/// Uses per-assembly info computed by <see cref="WasmCollectImportsExports"/> to emit C code corresponding to
/// the imports/exports on a set of assemblies. MSBuild can skip calling this if the existing generated code is
/// already newer than all of the per-assembly files.
/// </summary>
public class WasmGenerateImportsExports : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] Assemblies { get; set; } = default!;

    // For any pinvoke targeting one of these modules, we assume there will be a matching symbol in the compilation
    // For all other pinvokes, we will emit it as a WASM import
    [Required, NotNull]
    public string[]? LinkedModules { get; set; }

    [Output]
    public string? GeneratedCode { get; set; }

    public override bool Execute()
    {
        if (Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(WasmGenerateImportsExports)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        var resolver = new PathAssemblyResolver(Assemblies.Select(a => a.ItemSpec).ToList());
        using var metadataLoadContext = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        var jsonOptions = AssemblyImportExportInfo.CreateSerializerOptions(metadataLoadContext);
        var allPinvokes = new List<PInvoke>();
        var allPinvokeCallbacks = new List<PInvokeCallback>();
        var allSignatures = new List<string>();

        foreach (var assemblyItem in Assemblies)
        {
            // As a minor speed-up, skip empty files. These signify "no imports/exports"
            var assemblyGeneratedFilePath = assemblyItem.GetMetadata("GeneratedSource")
                ?? throw new InvalidOperationException($"Item '{assemblyItem.ItemSpec}' lacks required metadata 'GeneratedSource'");
            if (new FileInfo(assemblyGeneratedFilePath).Length == 0)
            {
                continue;
            }

            using var assemblyFileStream = File.OpenRead(assemblyGeneratedFilePath);
            var assemblyInfo = JsonSerializer.Deserialize<AssemblyImportExportInfo>(assemblyFileStream, jsonOptions)!;
            allPinvokes.AddRange(assemblyInfo.PInvokes);
            allPinvokeCallbacks.AddRange(assemblyInfo.PInvokeCallbacks);
            allSignatures.AddRange(assemblyInfo.Signatures);
        }

        using var outStream = new MemoryStream();
        using var outStreamWriter = new StreamWriter(outStream) {  AutoFlush = true };
        var pinvokeModules = LinkedModules.ToDictionary(x => x, x => x);
        PInvokeTableGenerator.EmitPInvokeTable(Log, outStreamWriter, pinvokeModules, allPinvokes, generateImportsForUnmatchedModules: true);
        GeneratedCode = Encoding.UTF8.GetString(outStream.GetBuffer(), 0, (int)outStream.Length);

        return true;
    }
}
