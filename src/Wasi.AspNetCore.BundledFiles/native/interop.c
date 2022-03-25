#include <mono-wasi/driver.h>

const char* dotnet_wasi_getbundledfile(const char* name, int* out_length);

const char* mono_get_embedded_file(MonoString* name, int* out_length) {
    char* name_utf8 = mono_wasm_string_get_utf8(name);
    return dotnet_wasi_getbundledfile(name_utf8, out_length);
}

void bundled_files_attach_internal_calls() {
    mono_add_internal_call("Wasi.AspNetCore.BundledFiles.WasiBundledFileProvider::GetEmbeddedFile", mono_get_embedded_file);
}
