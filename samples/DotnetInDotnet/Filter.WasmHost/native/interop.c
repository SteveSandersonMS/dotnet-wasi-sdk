#include <mono-wasi/driver.h>
#include <assert.h>
#include "dotnet_method.h"

void noop_settimeout(int timeout) {
    // Not implemented
}

DEFINE_DOTNET_METHOD(invoke_threadpool_callback, "System.Private.CoreLib.dll", "System.Threading", "ThreadPool", "Callback");
void wasi_queuecallback() {
    invoke_threadpool_callback(NULL, NULL);
}


MonoMethod* method_Apply;
MonoMethod* method_InstallFilter;

void native_attach() {
    mono_add_internal_call("System.Threading.TimerQueue::SetTimeout", noop_settimeout);
    mono_add_internal_call("System.Threading.ThreadPool::QueueCallback", wasi_queuecallback);

    method_Apply = lookup_dotnet_method("Filter.WasmHost.dll", "Filter.WasmHost", "InnerHost", "Apply", -1);
    assert(method_Apply);
    method_InstallFilter = lookup_dotnet_method("Filter.WasmHost.dll", "Filter.WasmHost", "InnerHost", "InstallFilter", -1);
    assert(method_InstallFilter);
}

__attribute__((export_name("loadDll")))
void loadDll(void* dllName, const unsigned char *dllDataPtr, unsigned int dllDataLenght) {
    assert(mono_wasm_add_assembly (dllName, dllDataPtr, dllDataLenght));
    free(dllName);
}

__attribute__((export_name("installFilter")))
int installFilter(void* filterNamePtr,unsigned int filterNameLength, unsigned int bufferLength) {
    void* method_params[] = { filterNamePtr, &filterNameLength, &bufferLength };
    MonoObject* exception;
    MonoObject* res = mono_wasm_invoke_method(method_InstallFilter, NULL, method_params, &exception);
    assert(!exception);
    assert(res);
    free(filterNamePtr);
    int ptr=mono_unbox_int(res);
    assert(ptr);
    return ptr;
}

__attribute__((export_name("apply")))
int apply(int lenghtBytes) {
    void* method_params[] = { &lenghtBytes };
    MonoObject* exception;
    MonoObject* res = mono_wasm_invoke_method(method_Apply, NULL, method_params, &exception);
    assert(!exception);
    return mono_unbox_int(res);
}
