#include <mono-wasi/driver.h>
#include <assert.h>
#include "dotnet_method.h"

void tcp_listener_attach_internal_calls();

void noop_settimeout(int timeout) {
    // Not implemented
}

DEFINE_DOTNET_METHOD(invoke_threadpool_callback, "System.Private.CoreLib.dll", "System.Threading", "ThreadPool", "Callback");
void wasi_queuecallback() {
    invoke_threadpool_callback(NULL, NULL);
}

void native_networking_attach_internal_calls() {
    mono_add_internal_call("System.Threading.TimerQueue::SetTimeout", noop_settimeout);
    mono_add_internal_call("System.Threading.ThreadPool::QueueCallback", wasi_queuecallback);
    tcp_listener_attach_internal_calls();
}
