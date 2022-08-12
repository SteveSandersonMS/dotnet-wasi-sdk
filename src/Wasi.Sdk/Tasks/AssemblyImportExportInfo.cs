// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private static void ReadToken(ref Utf8JsonReader reader, JsonTokenType assertType)
    {
        if (reader.TokenType != assertType)
        {
            throw new InvalidOperationException($"Expected token type {assertType} but found {reader.TokenType}");
        }

        reader.Read();
    }

    private static void ReadPropertyName(ref Utf8JsonReader reader, string propertyName)
    {
        var stringValue = reader.GetString();
        if (!string.Equals(stringValue, propertyName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected property name {propertyName} but found {stringValue}");
        }

        ReadToken(ref reader, JsonTokenType.PropertyName);
    }

    public static JsonSerializerOptions CreateSerializerOptions(MetadataLoadContext metadataLoadContext)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new PInvokeConverter(metadataLoadContext),
                new PInvokeCallbackConverter(metadataLoadContext),
            }
        };
    }

    static string GetOverloadIdentifier(MethodInfo methodInfo)
    {
        var sb = new StringBuilder();
        foreach (var param in methodInfo.GetParameters())
        {
            sb.Append(param.ParameterType.FullName);
            sb.Append(',');
        }
        return sb.ToString();
    }

    public class PInvokeConverter : JsonConverter<PInvoke>
    {
        private readonly MetadataLoadContext _metadataLoadContext;

        public PInvokeConverter(MetadataLoadContext metadataLoadContext)
        {
            _metadataLoadContext = metadataLoadContext;
        }

        public override PInvoke? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ReadToken(ref reader, JsonTokenType.StartObject);
            
            ReadPropertyName(ref reader, nameof(PInvoke.EntryPoint));
            var entryPoint = reader.GetString();
            reader.Read();

            ReadPropertyName(ref reader, nameof(PInvoke.Module));
            var module = reader.GetString();
            reader.Read();

            ReadPropertyName(ref reader, nameof(PInvoke.Method));
            ReadToken(ref reader, JsonTokenType.StartArray);
            var methodModuleName = reader.GetString();
            reader.Read();
            var methodDeclaringTypeName = reader.GetString();
            reader.Read();
            var methodName = reader.GetString();
            reader.Read();
            var overloadIdentifier = reader.GetString();
            reader.Read();
            ReadToken(ref reader, JsonTokenType.EndArray);

            var skip = false;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                ReadPropertyName(ref reader, nameof(PInvoke.Skip));
                skip = reader.GetBoolean();
                reader.Read();
            }

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new InvalidOperationException($"Should be at end of object, but found token {reader.TokenType}");
            }

            var assembly = _metadataLoadContext.LoadFromAssemblyName(methodModuleName!);
            var type = assembly.GetType(methodDeclaringTypeName!)!;
            var overloads = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal));
            var methodInfo = overloads.Single(m => GetOverloadIdentifier(m) == overloadIdentifier);

            return new PInvoke(entryPoint!, module!, methodInfo) { Skip = skip };
        }

        public override void Write(Utf8JsonWriter writer, PInvoke value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(value.EntryPoint), value.EntryPoint);
            writer.WriteString(nameof(value.Module), value.Module);

            // Not actually sure we'll be using this info - might be possible to skip emitting it
            writer.WritePropertyName(nameof(value.Method));
            writer.WriteStartArray();
            writer.WriteStringValue(value.Method.Module.Assembly.GetName().Name);
            writer.WriteStringValue(value.Method.DeclaringType!.FullName);
            writer.WriteStringValue(value.Method.Name);
            writer.WriteStringValue(GetOverloadIdentifier(value.Method));
            writer.WriteEndArray();

            if (value.Skip)
            {
                writer.WriteBoolean(nameof(value.Skip), value.Skip);
            }

            writer.WriteEndObject();
        }
    }

    public class PInvokeCallbackConverter : JsonConverter<PInvokeCallback>
    {
        private readonly MetadataLoadContext _metadataLoadContext;

        public PInvokeCallbackConverter(MetadataLoadContext metadataLoadContext)
        {
            _metadataLoadContext = metadataLoadContext;
        }

        public override PInvokeCallback? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ReadToken(ref reader, JsonTokenType.StartObject);

            ReadPropertyName(ref reader, nameof(PInvokeCallback.EntryName));
            var entryName = reader.GetString();
            reader.Read();

            ReadPropertyName(ref reader, nameof(PInvoke.Method));
            ReadToken(ref reader, JsonTokenType.StartArray);
            var methodModuleName = reader.GetString();
            reader.Read();
            var methodDeclaringTypeName = reader.GetString();
            reader.Read();
            var methodName = reader.GetString();
            reader.Read();
            var overloadIdentifier = reader.GetString();
            reader.Read();
            ReadToken(ref reader, JsonTokenType.EndArray);

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new InvalidOperationException($"Should be at end of object, but found token {reader.TokenType}");
            }

            var assembly = _metadataLoadContext.LoadFromAssemblyName(methodModuleName!);
            var type = assembly.GetType(methodDeclaringTypeName!)!;
            var overloads = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal));
            var methodInfo = overloads.Single(m => GetOverloadIdentifier(m) == overloadIdentifier);

            return new PInvokeCallback(methodInfo) { EntryName = entryName };
        }

        public override void Write(Utf8JsonWriter writer, PInvokeCallback value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(value.EntryName), value.EntryName);

            // Not actually sure we'll be using this info - might be possible to skip emitting it
            writer.WritePropertyName(nameof(value.Method));
            writer.WriteStartArray();
            writer.WriteStringValue(value.Method.Module.Assembly.GetName().Name);
            writer.WriteStringValue(value.Method.DeclaringType!.FullName);
            writer.WriteStringValue(value.Method.Name);
            writer.WriteStringValue(GetOverloadIdentifier(value.Method));
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
