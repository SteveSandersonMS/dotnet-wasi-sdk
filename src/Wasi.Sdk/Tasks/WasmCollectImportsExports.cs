// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Wasi.Sdk.Tasks;

/// <summary>
/// Scans a set of assemblies to locate import/export declarations, and generates the WASI SDK-compatible
/// C code to wire them up.
/// </summary>
public class WasmCollectImportsExports : Microsoft.Build.Utilities.Task
{
    // Each pinvoke is put into one of these three categories:
    // - If its module is in LinkedModules, we emit it as a regular C extern symbol, and hence the compilation will only
    //   succeed if we actually do link with a module exporting this symbol. This is used for things like libSystem.Native
    //   or if you were to link with sqlite etc.
    // - If its module matches an assembly metadata attribute called WasmImportModule, then we emit a Clang-attributed
    //   wasm import, so compilation doesn't require the symbol to existing, but we'd fail to start at runtime if the
    //   WASM host didn't supply a corresponding import
    // - For everything else, we skip it. This will be the case for many things referenced from framework assemblies that
    //   are never used in a real wasm app, such as Microsoft.AspNetCore.Server.IIS having DllImports to things on
    //   api-ms-win-core-io-l1-1-0.dll, etc. Most of these DllImports would be stripped out by trimming, but we want things
    //   to work even if you're not trimming.

    [Required]
    public ITaskItem[] Assemblies { get; set; } = default!;

    [Required, NotNull]
    public string[]? LinkedModules { get; set; }

    public override bool Execute()
    {
        if (Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(WasmCollectImportsExports)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        var resolver = new PathAssemblyResolver(Assemblies.Select(a => a.ItemSpec).ToList());
        using var metadataLoadContext = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        var jsonOptions = AssemblyImportExportInfo.CreateSerializerOptions(metadataLoadContext);

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
            var extractedInfo = new AssemblyImportExportInfo();
            var modulesForAssembly = GetModulesForAssembly(assembly, LinkedModules);
            foreach (var type in assembly.GetTypes())
            {
                PInvokeTableGenerator.CollectPInvokes(Log, extractedInfo.PInvokes, extractedInfo.PInvokeCallbacks, extractedInfo.Signatures, modulesForAssembly, type);
            }

            // Finally, emit the per-assembly info into a file
            Directory.CreateDirectory(Path.GetDirectoryName(assemblyGeneratedFilePath)!);
            using var assemblyIntermediateFileStream = File.OpenWrite(assemblyGeneratedFilePath);
            if (!extractedInfo.IsEmpty)
            {
                JsonSerializer.Serialize(assemblyIntermediateFileStream, extractedInfo, jsonOptions);
            }
        }

        return true;
    }

    private Dictionary<string, string> GetModulesForAssembly(Assembly assembly, string[] linkedModules)
    {
        // We'll include both:
        // - the global set of linked modules, which will result in generating regular C extern symbols that have
        //   to be resolved at link time
        // - and any WasmImportModule values on the assembly, which will result in generating WebAssembly imports
        //   that get resolved at module instantiation time
        var wasmImportModules = CustomAttributeData.GetCustomAttributes(assembly)
            .Where(a => string.Equals(a.AttributeType.FullName, typeof(AssemblyMetadataAttribute).FullName, StringComparison.Ordinal))
            .Where(a => a.ConstructorArguments.Count == 2 && string.Equals((string)a.ConstructorArguments[0].Value!, "WasmImportModule", StringComparison.OrdinalIgnoreCase))
            .Where(a => !string.IsNullOrWhiteSpace((string)a.ConstructorArguments[1].Value!))
            .Select(a => (string)a.ConstructorArguments[1].Value!);

        return linkedModules.Concat(wasmImportModules).ToDictionary(x => x, x => x);
    }
}
