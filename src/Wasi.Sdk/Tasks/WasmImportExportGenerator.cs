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

            var assemblyName = assemblyItem.ItemSpec;
            var assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyName);

            var assemblyGeneratedSource = new StringBuilder();
            assemblyGeneratedSource.AppendLine("// This is generated code. Do not edit.");
            assemblyGeneratedSource.AppendLine();

            foreach (var type in assembly.GetTypes())
            {
                GenerateImportsExportsForType(type, assemblyGeneratedSource);
            }

            File.WriteAllText(assemblyGeneratedFilePath, assemblyGeneratedSource.ToString());
        }

        // TODO: Generate registration code based on which files already existed and which ones we just wrote
        // some nonempty content into
        var registrationCode = new StringBuilder();
        registrationCode.AppendLine("// This is generated code. Do not edit.");
        registrationCode.AppendLine();
        ImportExportRegistrationSourceCode = registrationCode.ToString();

        return true;
    }

    private void GenerateImportsExportsForType(Type type, StringBuilder assemblyGeneratedSource)
    {
        // Only considering static methods, since there's no clear use case for instance methods as imports/exports,
        // and people will get confused about how native code should invoke an instance method
        foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            try
            {
                GenerateImportsForMethod(assemblyGeneratedSource, method);
                GenerateExportsForMethod(assemblyGeneratedSource, method);
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.High, $"Could not scan for imports/exports in {method.Name}: {ex}");
                continue;
            }
        }
    }

    private static void GenerateImportsForMethod(StringBuilder generatedCode, MethodInfo method)
    {
        // Look for [DllImport]
        if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
        {
            var dllimport = CustomAttributeData.GetCustomAttributes(method).First(attr => attr.AttributeType.Name == "DllImportAttribute");
            var module = (string)dllimport.ConstructorArguments[0].Value!;
            var entrypoint = (string)dllimport.NamedArguments.First(arg => arg.MemberName == "EntryPoint").TypedValue.Value!;

            var signature = SignatureMapper.MethodToSignature(method)
                ?? throw new LogAsErrorException($"Unsupported parameter type in method '{method.DeclaringType!.FullName}.{method.Name}'");

            generatedCode.AppendLine($"Found IMPORT of {module}.{entrypoint} from .NET method {method.DeclaringType!.FullName}::{method.Name}, signature {signature}");
        }
    }

    private static void GenerateExportsForMethod(StringBuilder generatedCode, MethodInfo method)
    {
        // One way we can speed this up is to require export methods to be public. This makes sense because
        // you're letting some external code call it. We won't require the containing type to be public though.
        // Speed isn't actually critical because we have per-assembly incrementalism.
        if (method.IsPublic)
        {
            foreach (var customAttribute in CustomAttributeData.GetCustomAttributes(method))
            {
                // We don't reference the Wasi.Runtime assembly here, because doing so would require it to target netstandard2.0,
                // and it's not desirable for that to be in the public API. So the type names have to be hardcoded as strings.
                if (string.Equals("Wasi.Runtime.WasmExportAttribute", customAttribute.AttributeType.FullName, StringComparison.Ordinal))
                {
                    generatedCode.AppendLine($"Found EXPORT method {method.DeclaringType!.FullName}::{method.Name}");
                }
            }
        }
    }
}
