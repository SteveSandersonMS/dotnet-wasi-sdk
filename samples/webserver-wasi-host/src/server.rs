use tiny_http::Header;
use wasmtime_wasi::WasiCtx;
use anyhow::*;
use tiny_http::{Server, Request};
use wasmtime::*;
use std::collections::HashMap;

pub struct RequestContext {
    request: Request,
    response_headers: Vec<Header>,
    response_chunks: Vec<Vec<u8>>,
}

pub struct DotNetHttpServerStore {
    wasi_ctx: WasiCtx,
    next_request_id: u32,
    pending_requests: HashMap::<u32, RequestContext>,
}

impl DotNetHttpServerStore {
    pub fn new(wasi_ctx: WasiCtx) -> DotNetHttpServerStore {
        DotNetHttpServerStore { wasi_ctx, next_request_id: 1, pending_requests: HashMap::new() }
    }

    pub fn wasi_ctx_mut(&mut self) -> &mut WasiCtx {
        &mut self.wasi_ctx
    }

    pub fn push_response_header(&mut self, request_id: u32, header: Header) {
        if let Some(request_context) = self.pending_requests.get_mut(&request_id) {
            request_context.response_headers.push(header);
        } else {
            println!("WARNING: No such pending request {}", request_id);
        }
    }

    pub fn push_response_chunk(&mut self, request_id: u32, chunk: Vec<u8>) {
        // tiny_http doesn't seem to support streaming responses, so we have to collect all the chunks in memory
        // until we're ready to respond. I should remove tiny_http and switch either to a better high-level HTTP
        // framework, or just build directly on epoll-like APIs.
        if let Some(request_context) = self.pending_requests.get_mut(&request_id) {
            request_context.response_chunks.push(chunk);
        } else {
            println!("WARNING: No such pending request {}", request_id);
        }
    }

    pub fn complete_response(&mut self, request_id: u32, status_code: i32) {
        if let Some(mut request_context) = self.pending_requests.remove(&request_id) {
            let all_chunks = request_context.response_chunks.concat();
            let mut response = tiny_http::Response::from_data(all_chunks).with_status_code(status_code);

            while let Some(h) = request_context.response_headers.pop() {
                response.add_header(h);
            }

            request_context.request.respond(response).expect("Failed to send response");
        }
    }
}

pub struct DotNetHttpServer<'a> {
    caller: wasmtime::Caller<'a, DotNetHttpServerStore>,
    wasm_on_incoming_request: TypedFunc<(i32, u32, i32, i32, i32, i32, i32), ()>,
    wasm_malloc: TypedFunc<i32, i32>,
    wasm_memory: wasmtime::Memory,
}

impl DotNetHttpServer<'_> {
    pub fn add_to_linker(linker: &mut Linker<DotNetHttpServerStore>) -> Result<()> {
        linker.func_wrap("env", "start_http_server", |caller: Caller<DotNetHttpServerStore>, dotnet_http_server: i32, port: i32| {
            crate::server::launch_server(u16::try_from(port).unwrap(), dotnet_http_server, caller);
        })?;
    
        linker.func_wrap("env", "response_send_chunk", |mut caller: Caller<DotNetHttpServerStore>, request_id: u32, buffer_ptr: i32, buffer_len: i32| {
            let bytes: Vec<u8> = {
                let wasm_memory = caller.get_export("memory").expect("Missing export 'memory'").into_memory().unwrap();
                let mut store = caller.as_context_mut();
                let bytes = wasm_memory.data(&mut store).get((buffer_ptr as usize)..((buffer_ptr+buffer_len) as usize)).unwrap();
                bytes.to_vec()
            };
            caller.data_mut().push_response_chunk(request_id, bytes);
        })?;
    
        linker.func_wrap("env", "response_add_header", |mut caller: Caller<DotNetHttpServerStore>, request_id: u32, name_ptr: i32, name_len: i32, value_ptr: i32, value_len: i32| {
            let (name, value) = {
                let wasm_memory = caller.get_export("memory").expect("Missing export 'memory'").into_memory().unwrap();
                let mut store = caller.as_context_mut();
                let heap = wasm_memory.data(&mut store);
                let name = heap.get((name_ptr as usize)..((name_ptr+name_len) as usize)).unwrap();
                let value = heap.get((value_ptr as usize)..((value_ptr+value_len) as usize)).unwrap();
                (name.to_vec(), value.to_vec())
            };
            caller.data_mut().push_response_header(request_id, tiny_http::Header::from_bytes(name, value).unwrap());
        })?;
    
        linker.func_wrap("env", "response_complete", |mut caller: Caller<DotNetHttpServerStore>, request_id: u32, status_code: i32| {
            caller.data_mut().complete_response(request_id, status_code);
        })?;

        Ok(())
    }

    fn new(mut caller: wasmtime::Caller<DotNetHttpServerStore>) -> DotNetHttpServer {
        let wasm_on_incoming_request = caller.get_export("on_incoming_request").expect("Missing export 'on_incoming_request'");
        let wasm_malloc = caller.get_export("malloc").expect("Missing export 'malloc'");
        let wasm_memory = caller.get_export("memory").expect("Missing export 'memory'").into_memory().unwrap();

        let mut store = caller.as_context_mut();

        let wasm_on_incoming_request = wasm_on_incoming_request.into_func().unwrap().typed::<(i32, u32, i32, i32, i32, i32, i32), (), _>(&mut store).expect("Type mismatch for 'on_incoming_request'");
        let wasm_malloc = wasm_malloc.into_func().unwrap().typed::<i32, i32, _>(&mut store).expect("Type mismatch for 'malloc'");

        DotNetHttpServer {
            caller,
            wasm_on_incoming_request,
            wasm_malloc,
            wasm_memory,
        }
    }

    fn store_data_mut(&mut self) -> &mut DotNetHttpServerStore {
        self.caller.data_mut()
    }

    fn on_incoming_request(&mut self, dotnet_http_server: i32, mut request: Request) -> Result<()> {
        let method_str = self.create_wasm_string(request.method().as_str())?;
        let url_str = self.create_wasm_string(request.url())?;

        let mut headers_combined = String::new();
        for header in request.headers() {
            headers_combined.push_str(header.field.as_str().as_str());
            headers_combined.push_str(":");
            headers_combined.push_str(header.value.as_str());
            headers_combined.push_str("\n");
        }
        let headers_combined_str = self.create_wasm_string(headers_combined.as_str())?;

        let mut body_buf = Vec::<u8>::new();
        let body_buf_len = request.as_reader().read_to_end(&mut body_buf).unwrap();
        let body_buf_ptr = self.create_wasm_ptr(&body_buf)?;

        let store_data_mut = self.store_data_mut();
        let request_id = store_data_mut.next_request_id;
        store_data_mut.next_request_id += 1;

        store_data_mut.pending_requests.insert(request_id, RequestContext {
            request,
            response_chunks: Vec::<Vec<u8>>::new(),
            response_headers: Vec::<Header>::new(),
        });

        let mut store = self.caller.as_context_mut();
        self.wasm_on_incoming_request.call(&mut store, (dotnet_http_server, request_id, method_str, url_str, headers_combined_str, body_buf_ptr, body_buf_len as i32))?;
        Ok(())
    }

    fn create_wasm_string(&mut self, value: &str) -> Result<i32> {
        let value_utf8 = value.as_bytes();
        self.create_wasm_ptr(value_utf8)
    }

    fn create_wasm_ptr(&mut self, value: &[u8]) -> Result<i32> {
        let value_len = value.len();
        let mut store = self.caller.as_context_mut();
        let wasm_ptr = self.wasm_malloc.call(&mut store, value_len as i32 + 1)?;
        self.wasm_memory.write(&mut store, wasm_ptr as usize, value)?;
        self.wasm_memory.write(&mut store, (wasm_ptr as usize) + value_len, &[0])?;
        Ok(wasm_ptr)
    }
}

pub fn launch_server(port: u16, dotnet_http_server: i32, caller: wasmtime::Caller<'_, DotNetHttpServerStore>) -> () {
    run_server(port, dotnet_http_server, caller).ok();
}

fn run_server(port: u16, dotnet_http_server: i32, caller: wasmtime::Caller<'_, DotNetHttpServerStore>) -> Result<()> {
    let mut dotnet = DotNetHttpServer::new(caller);

    let server = Server::http(format!("0.0.0.0:{}", port)).unwrap();

    for request in server.incoming_requests() {
        dotnet.on_incoming_request(dotnet_http_server, request)?;
    }

    Ok(())
}
