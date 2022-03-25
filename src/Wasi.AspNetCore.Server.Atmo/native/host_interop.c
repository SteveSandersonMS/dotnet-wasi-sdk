#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <mono-wasi/driver.h>

__attribute__((import_name("log_msg")))
void log_msg(const char* msg, int msg_len, int log_level, int ident);

__attribute__((import_name("return_result")))
void return_result(void* buf, int buf_len, int ident);

__attribute__((import_name("return_error")))
void return_error(int code, void* buf, int buf_len, int ident);

__attribute__((import_name("request_get_field")))
int request_get_field(int field_type, const char* key_ptr, int key_len, int ident);

__attribute__((import_name("resp_set_header")))
void resp_set_header(const char* key_ptr, int key_len, const char* val_ptr, int val_len, int ident);

__attribute__((import_name("get_ffi_result")))
int get_ffi_result(void* buf, int ident);

__attribute__((import_name("cache_get")))
int cache_get(const char* key_ptr, int key_len, int ident);

__attribute__((import_name("cache_set")))
int cache_set(const char* key_ptr, int key_len, void* value, int value_len, int ttl, int ident);

__attribute__((export_name("allocate")))
void* allocate(int length) {
    return malloc(length + 1);
}

__attribute__((export_name("deallocate")))
void deallocate(void* ptr, int length) {
    free(ptr);
}

char* request_get_field_str(int field_type, const char* field_name, int ident) {
    int ffi_res_len = request_get_field(field_type, field_name, strlen(field_name), ident);
    char* ffi_res_buf = (char*)malloc(ffi_res_len + 1);
    assert(!get_ffi_result(ffi_res_buf, ident));
    ffi_res_buf[ffi_res_len] = 0;
    return ffi_res_buf;
}

MonoString* request_get_field_mono(int field_type, MonoString* field_name, int ident) {
    char* field_name_utf8 = mono_wasm_string_get_utf8(field_name);
    char* result_utf8 = request_get_field_str(field_type, field_name_utf8, ident);

    MonoString* result_monostring = mono_wasm_string_from_js(result_utf8);

    free(field_name_utf8);
    free(result_utf8);

    return result_monostring;
}

MonoMethod* method_HandleIncomingRequest;
MonoObject* interop_instance = 0;

#define FIELD_TYPE_META 0
#define FIELD_TYPE_BODY 1
#define FIELD_TYPE_HEADER 2
#define FIELD_TYPE_PARAMS 3
#define FIELD_TYPE_STATE 4

__attribute__((export_name("run_e")))
void run_e(char* buf, int buf_len, int ident) {
    if (!method_HandleIncomingRequest) {
        method_HandleIncomingRequest = lookup_dotnet_method("Wasi.AspNetCore.Server.Atmo.dll", "Wasi.AspNetCore.Server.Atmo", "Interop", "HandleIncomingRequest", -1);
        assert(method_HandleIncomingRequest);
    }

    void* method_params[] = { interop_instance, &ident };
    MonoObject* exception;
    mono_wasm_invoke_method(method_HandleIncomingRequest, NULL, method_params, &exception);
    assert(!exception);
}

void atmo_log_message(int ident, int level, MonoString* message) {
    char* message_utf8 = mono_wasm_string_get_utf8(message);
    log_msg(message_utf8, strlen(message_utf8), level, ident);
    free(message_utf8);
}

void atmo_log_message_raw(int ident, int level, char* message, int message_len) {
    log_msg(message, message_len, level, ident);
}

void fake_settimeout(int timeout) {
    // Skipping
}

void run_http_server(MonoObject* interop, int port) {
    // Ignoring the port because that's decided by Atmo
    interop_instance = interop;
}

void response_add_header_mono(int ident, MonoString* name, MonoString* value) {
    char* name_utf8 = mono_wasm_string_get_utf8(name);
    char* value_utf8 = mono_wasm_string_get_utf8(value);
    resp_set_header(name_utf8, strlen(name_utf8), value_utf8, strlen(value_utf8), ident);
}

void response_complete(int ident, int status_code, char* body, int body_len) {
    // The ABI only lets us set a custom status code if we're treating the result as an error
    if (status_code >= 200 && status_code < 300) {
        return_result(body, body_len, ident);
    } else {
        return_error(status_code, body, body_len, ident);
    }
}

MonoClass* mono_get_byte_class(void);
MonoDomain* mono_get_root_domain(void);

MonoArray* mono_wasm_typed_array_new(char* arr, int length) {
    MonoClass* typeClass = mono_get_byte_class();
    MonoArray* buffer = mono_array_new(mono_get_root_domain(), typeClass, length);
    memcpy(mono_array_addr_with_size(buffer, sizeof(char), 0), arr, length);
    return buffer;
}

MonoArray* mono_cache_get(int ident, MonoString* key) {
    char* key_utf8 = mono_wasm_string_get_utf8(key);
    int ffi_res_len = cache_get(key_utf8, strlen(key_utf8), ident);
    if (ffi_res_len < 0) {
        // TODO: If it's less than -1, it's the size needed for a string whose value is an error message
        // Get this and return it to .NET - see https://github.com/suborbital/reactr/blob/6ab5699f12d947b769368bf8d7f90eb7b7acd950/api/assemblyscript/assembly/ffi.ts#L32
        return 0;
    }

    char* ffi_res_buf = (char*)malloc(ffi_res_len);
    assert(!get_ffi_result(ffi_res_buf, ident));
    MonoArray* dotnet_byte_array = mono_wasm_typed_array_new(ffi_res_buf, ffi_res_len);
    free(ffi_res_buf);

    return dotnet_byte_array;
}

int mono_cache_set(int ident, MonoString* key, char* value, int value_len, int ttl) {
    char* key_utf8 = mono_wasm_string_get_utf8(key);
    int res = cache_set(key_utf8, strlen(key_utf8), value, value_len, ttl, ident);
    free(key_utf8);
    return res;
}

void atmo_attach_internal_calls() {
    mono_add_internal_call("Wasi.AspNetCore.Server.Atmo.Interop::LogMessage", atmo_log_message);
    mono_add_internal_call("Wasi.AspNetCore.Server.Atmo.Interop::LogMessageRaw", atmo_log_message_raw);
    mono_add_internal_call("Wasi.AspNetCore.Server.Atmo.Interop::RunHttpServer", run_http_server);
    mono_add_internal_call("Wasi.AspNetCore.Server.Atmo.Interop::RequestGetField", request_get_field_mono);    
    mono_add_internal_call("Wasi.AspNetCore.Server.Atmo.Interop::ResponseAddHeader", response_add_header_mono);
    mono_add_internal_call("Wasi.AspNetCore.Server.Atmo.Interop::ResponseComplete", response_complete);
    mono_add_internal_call("Wasi.AspNetCore.Server.Atmo.Services.Cache::Get", mono_cache_get);
    mono_add_internal_call("Wasi.AspNetCore.Server.Atmo.Services.Cache::Set", mono_cache_set);
    mono_add_internal_call("System.Threading.TimerQueue::SetTimeout", fake_settimeout);
}
