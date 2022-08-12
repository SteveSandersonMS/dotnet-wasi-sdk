﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

    [Output]
    public string? GeneratedCode { get; set; }

    public override bool Execute()
    {
        if (Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(Assemblies)} cannot be empty");
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

            Log.LogMessage(MessageImportance.High, $"Parsed {assemblyInfo.Signatures.Count} signatures from {assemblyGeneratedFilePath}");
        }

        return true;
    }
}
