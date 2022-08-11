// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Wasi.Sdk.Tasks;

// TODO: Rename this to WasmCollectImportExports and have it only responsible for emitting .json data incrementally
// Then add a second task WasmGenerateImportExports that incrementally only runs if any of the .json files have changed
// and calls PInvokeGenerator's EmitPInvokeTable to write out a single .c file
//
// Also need to change EmitPInvokeTable so that, for unknown modules, it emits a proper Clang "import" function
// so it ends up as a wam import.
//
// Haven't decided how to handle exports - might either add some support for detecting them into CollectPInvokes,
// or could have a whole separate process for scanning for them.

/// <summary>
/// Scans a set of assemblies to locate import/export declarations, and generates the WASI SDK-compatible
/// C code to wire them up.
/// </summary>
public class WasmImportExportGenerator : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] Assemblies { get; set; } = default!;

    [Output]
    public string? ImportExportRegistrationSourceCode { get; set; }

    public override bool Execute()
    {
        if (Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        var resolver = new PathAssemblyResolver(Assemblies.Select(a => a.ItemSpec).ToList());
        using var metadataLoadContext = new MetadataLoadContext(resolver, "System.Private.CoreLib");

        foreach (var assemblyItem in Assemblies)
        {
            // If the per-assembly generated file already exists, we can skip the assembly. The generated file path
            // includes a content hash, so its existence shows we're up-to-date
            var assemblyGeneratedFilePath = assemblyItem.GetMetadata("GeneratedSource")
                ?? throw new InvalidOperationException($"Item '{assemblyItem.ItemSpec}' lacks required metadata 'GeneratedSource'");
            if (File.Exists(assemblyGeneratedFilePath))
            {
                continue;
            }

            // Now call into the runtime's regular PInvokeTableGenerator to collect the per-assembly info
            var assemblyName = assemblyItem.ItemSpec;
            var assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyName);
            var assemblyPinvokes = new List<PInvoke>();
            var assemblyPinvokeCallbacks = new List<PInvokeCallback>();
            var assemblySignatures = new List<string>();
            foreach (var type in assembly.GetTypes())
            {
                PInvokeTableGenerator.CollectPInvokes(Log, assemblyPinvokes, assemblyPinvokeCallbacks, assemblySignatures, type);
            }

            var assemblyImportExportInfo = new AssemblyImportExportInfo
            {
                Signatures = assemblySignatures
            };

            using var assemblyIntermediateFileStream = File.OpenWrite(assemblyGeneratedFilePath);
            JsonSerializer.Serialize(assemblyIntermediateFileStream, assemblyImportExportInfo);
        }

        return true;
    }

    private class AssemblyImportExportInfo
    {
        public List<string> Signatures { get; set; } = default!;
    }
}
