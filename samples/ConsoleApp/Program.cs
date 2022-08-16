using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyMetadata("WasmImportModule", "wasi_snapshot_preview1")]
[assembly: AssemblyMetadata("WasmImportModule", "mymodule")]

Console.WriteLine($"Hello, world at {DateTime.Now.ToLongTimeString()} on {RuntimeInformation.OSArchitecture}!");
