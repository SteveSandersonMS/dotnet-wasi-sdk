#define DEFINE_DOTNET_METHOD(c_name, assembly_name, namespc, class_name, method_name) \
MonoMethod* method_##c_name;\
MonoObject* c_name(MonoObject* target_instance, void* method_params[]) {\
    if (!method_##c_name) {\
        method_##c_name = lookup_dotnet_method(assembly_name, namespc, class_name, method_name, -1);\
        assert(method_##c_name);\
    }\
\
    MonoObject* exception;\
    MonoObject* res = mono_wasm_invoke_method(method_##c_name, target_instance, method_params, &exception);\
    assert(!exception);\
    return res;\
}
