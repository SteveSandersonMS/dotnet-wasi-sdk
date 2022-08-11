// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [Required, NotNull]
    public ITaskItem[] AssembliesToScan { get; set; } = default!;

    [Output]
    public string? GeneratedSourceCode { get; set; }

    public override bool Execute()
    {
        if (AssembliesToScan!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(AssembliesToScan)} cannot be empty");
            return false;
        }

        var assemblyNames = AssembliesToScan.Select(a => a.ItemSpec).ToList();
        var resolver = new PathAssemblyResolver(assemblyNames);
        using var metadataLoadContext = new MetadataLoadContext(resolver);

        var generatedCode = new StringBuilder();
        generatedCode.AppendLine("// This is generated code. Do not edit.");
        generatedCode.AppendLine();

        foreach (var assemblyName in assemblyNames)
        {
            var assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyName);

            var referencedAssemblies = assembly.GetReferencedAssemblies();
            if (!referencedAssemblies.Any(r => string.Equals("Wasi.Runtime", r.Name, StringComparison.Ordinal)))
            {
                // If there's no reference to Wasi.Runtime, skip scanning this assembly, since
                // we know it can't include any of the import/export attributes
                break;
            }

            foreach (var type in assembly.GetTypes())
            {
                GenerateImportsExportsForType(generatedCode, type);
            }
        }

        GeneratedSourceCode = generatedCode.ToString();
        return true;
    }

    private void GenerateImportsExportsForType(StringBuilder generatedCode, Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            try
            {
                GenerateImportsExportsForMethod(generatedCode, method);
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
