mod server;
use std::error::Error;
use std::path::Path;
use server::*;
use anyhow::Result;
use wasmtime::*;
use wasmtime_wasi::*;

fn main() -> Result<(), Box<dyn Error>> {
    let args: Vec<String> = std::env::args().collect();
    let wasm_file_to_execute = get_wasm_file_to_execute(args)?;
    
    // Define the WASI functions globally on the `Config`.
    let engine = Engine::default();
    let mut linker: Linker<DotNetHttpServerStore> = Linker::new(&engine);
    wasmtime_wasi::add_to_linker(&mut linker, |s| s.wasi_ctx_mut())?;
    DotNetHttpServer::add_to_linker(&mut linker)?;

    // Create a WASI context and put it in a Store; all instances in the store
    // share this context. `WasiCtxBuilder` provides a number of ways to
    // configure what the target program will have access to.
    let wasi = WasiCtxBuilder::new()
        .inherit_stdio()
        .preopened_dir(sync::Dir::open_ambient_dir(std::env::current_dir()?, sync::ambient_authority())?, ".")?
        .inherit_args()?

        // For security reasons, you might not really want to let the WASM-sandboxed code see all the environment
        // variables from the native host process. However it is a convenient way to pass through everything from
        // launchSettings.json.
        .env("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "")? // Disable watch (otherwise, "Startup assembly Microsoft.AspNetCore.Watch.BrowserRefresh failed to execute")
        .inherit_env()?

        .build();
    let mut store = Store::new(&engine, DotNetHttpServerStore::new(wasi));

    // Instantiate our module with the imports we've created, and run it.
    let module = Module::from_file(&engine, wasm_file_to_execute)?;
    linker.module(&mut store, "", &module)?;
    linker
        .get_default(&mut store, "")?
        .typed::<(), (), _>(&store)?
        .call(&mut store, ())?;

    Ok(())
}

fn get_wasm_file_to_execute(args: Vec<String>) -> Result<String, Box<dyn Error>> {
    if args.len() < 2
    {
        return Err(format!("Usage: {} file.wasm", args.get(0).unwrap()).into());
    }

    let wasm_file_to_execute = args.get(1).unwrap();
    if !Path::new(wasm_file_to_execute).exists()
    {
        return Err(format!("Could not find file {}", wasm_file_to_execute).into());
    }

    Ok(wasm_file_to_execute.to_string())
}
