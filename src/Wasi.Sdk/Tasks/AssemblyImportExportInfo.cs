// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wasi.Sdk.Tasks;

internal class AssemblyImportExportInfo
{
    public List<PInvoke> PInvokes { get; set; } = new();
    public List<PInvokeCallback> PInvokeCallbacks { get; set; } = new();
    public List<string> Signatures { get; set; } = new();

    [JsonIgnore]
    public bool IsEmpty => !(PInvokes.Any() || PInvokeCallbacks.Any() || Signatures.Any());

    public static JsonSerializerOptions CreateSerializerOptions(MetadataLoadContext metadataLoadContext)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new MetadataLoadContextMethodInfoConverter(metadataLoadContext)
            }
        };
    }

    class MetadataLoadContextMethodInfoConverter : JsonConverter<MethodInfo>
    {
        private MetadataLoadContext _metadataLoadContext;

        public MetadataLoadContextMethodInfoConverter(MetadataLoadContext metadataLoadContext)
        {
            _metadataLoadContext = metadataLoadContext;
        }

        public override void Write(Utf8JsonWriter writer, MethodInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.Module.Assembly.GetName().Name);
            writer.WriteStringValue(value.DeclaringType!.FullName);
            writer.WriteStringValue(value.Name);
            writer.WriteStringValue(GetOverloadIdentifier(value));
            writer.WriteEndArray();
        }

        public override MethodInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read(); // Move past StartArray
            var methodModuleName = reader.GetString();
            reader.Read();
            var methodDeclaringTypeName = reader.GetString();
            reader.Read();
            var methodName = reader.GetString();
            reader.Read();
            var overloadIdentifier = reader.GetString();
            reader.Read(); // Move to EndArray

            var assembly = _metadataLoadContext.LoadFromAssemblyName(methodModuleName!);
            var type = assembly.GetType(methodDeclaringTypeName!)!;
            var overloads = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal));
            var methodInfo = overloads.Single(m => GetOverloadIdentifier(m) == overloadIdentifier);
            return methodInfo;
        }

        private static string GetOverloadIdentifier(MethodInfo methodInfo)
        {
            var sb = new StringBuilder();
            foreach (var param in methodInfo.GetParameters())
            {
                sb.Append(param.ParameterType.FullName);
                sb.Append(',');
            }
            return sb.ToString();
        }
    }
}
