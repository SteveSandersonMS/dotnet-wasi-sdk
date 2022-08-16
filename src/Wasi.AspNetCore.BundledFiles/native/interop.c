#include <mono-wasi/driver.h>

const char* dotnet_wasi_getbundledfile(const char* name, int* out_length);

const char* aspnetcorebundledfiles_mono_get_embedded_file(MonoString** name, int* out_length) {
    char* name_utf8 = mono_wasm_string_get_utf8(*name);
    const char* result = dotnet_wasi_getbundledfile(name_utf8, out_length);
    free(name_utf8);
    return result;
}
