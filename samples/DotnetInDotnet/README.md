# Assembly isolation using WASI sandbox

This sample shows how to
- use [wasmtime-dotnet](https://github.com/bytecodealliance/wasmtime-dotnet) wrapper API around wasmtime engine
  - configure it for WASI
  - call exported functions
  - map memory
- load untrusted DLL into runtime inside of WASM process
- marshal json payload as bytes
- dispatch call to untrusted component


## TODO
- runtime fails on first GC
- dependencies of the untrusted DLL are not included