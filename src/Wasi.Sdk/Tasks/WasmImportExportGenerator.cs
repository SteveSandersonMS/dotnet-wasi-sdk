// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Wasi.Sdk.Tasks;

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
        using var metadataLoadContext = new MetadataLoadContext(resolver);

        var registrationCode = new StringBuilder();
        registrationCode.AppendLine("// This is generated code. Do not edit.");
        registrationCode.AppendLine();

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

            var assemblyName = assemblyItem.ItemSpec;
            var assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyName);
            
            var assemblyGeneratedSource = new StringBuilder();
            assemblyGeneratedSource.AppendLine("// This is generated code. Do not edit.");
            assemblyGeneratedSource.AppendLine();

            foreach (var type in assembly.GetTypes())
            {
                GenerateImportsExportsForType(type, registrationCode, assemblyGeneratedSource);
            }

            File.WriteAllText(assemblyGeneratedFilePath, assemblyGeneratedSource.ToString());
        }

        ImportExportRegistrationSourceCode = registrationCode.ToString();
        return true;
    }

    private void GenerateImportsExportsForType(Type type, StringBuilder registrationCode, StringBuilder assemblyGeneratedSource)
    {
        foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            try
            {
                GenerateImportsExportsForMethod(registrationCode, method);
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Could not scan for wasm imports/exports in {method.Name}: {ex}");
                continue;
            }
        }
    }

    private void GenerateImportsExportsForMethod(StringBuilder generatedCode, MethodInfo method)
    {
        foreach (var customAttribute in CustomAttributeData.GetCustomAttributes(method))
        {
            // We don't reference the Wasi.Runtime assembly here, because doing so would require it to target netstandard2.0,
            // and it's not desirable for that to be in the public API. So the type names have to be hardcoded as strings.

            if (string.Equals("Wasi.Runtime.WasmImportAttribute", customAttribute.AttributeType.FullName, StringComparison.Ordinal))
            {
                generatedCode.AppendLine($"Found method {method.DeclaringType!.FullName}::{method.Name} with attribute {customAttribute.AttributeType.FullName}");
            }

            if (string.Equals("Wasi.Runtime.WasmExportAttribute", customAttribute.AttributeType.FullName, StringComparison.Ordinal))
            {
                generatedCode.AppendLine($"Found method {method.DeclaringType!.FullName}::{method.Name} with attribute {customAttribute.AttributeType.FullName}");
            }
        }
    }
}
