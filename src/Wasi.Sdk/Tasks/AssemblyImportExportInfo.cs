// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                new PInvokeConverter(metadataLoadContext),
                new PInvokeCallbackConverter(metadataLoadContext),
            }
        };
    }

    public class PInvokeConverter : JsonConverter<PInvoke>
    {
        private readonly MetadataLoadContext _metadataLoadContext;

        public PInvokeConverter(MetadataLoadContext metadataLoadContext)
        {
            _metadataLoadContext = metadataLoadContext;
        }

        public override PInvoke? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => default;

        public override void Write(Utf8JsonWriter writer, PInvoke value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(value.EntryPoint), value.EntryPoint);
            writer.WriteString(nameof(value.Module), value.Module);

            // Not actually sure we'll be using this info - might be possible to skip emitting it
            writer.WritePropertyName(nameof(value.Method));
            writer.WriteStartArray();
            writer.WriteStringValue(value.Method.Module.Name);
            writer.WriteNumberValue(value.Method.MetadataToken);
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
            => default;

        public override void Write(Utf8JsonWriter writer, PInvokeCallback value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(value.EntryName), value.EntryName);

            // Not actually sure we'll be using this info - might be possible to skip emitting it
            writer.WritePropertyName(nameof(value.Method));
            writer.WriteStartArray();
            writer.WriteStringValue(value.Method.Module.Name);
            writer.WriteNumberValue(value.Method.MetadataToken);
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
