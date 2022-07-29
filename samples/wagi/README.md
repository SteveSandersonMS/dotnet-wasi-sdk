# .NET Samples for WAGI

These samples demonstrate writing .NET Web applications using WAGI (WebAssembly
Gateway Interface), an implementation of the well-known CGI protocol in
WebAssembly. While newer frameworks such as ASP.NET have largely superseded
CGI for languages that have them, WAGI provides a language-neutral,
zero-dependency option, which allows developers to reuse Wasm modules
regardless of their language of origin.

The demo is provided with manifest files for Wagi itself (https://github.com/deislabs/wagi)
and the Spin runtime (https://github.com/fermyon/spin), which implements the
WAGI protocol.

## Running the sample in Wagi

* Download Wagi from https://github.com/deislabs/wagi/releases and put it on your PATH
* Change to this directory
* Build the three projects by running `dotnet build`
* Run `wagi -c modules.toml`
* Visit `http://localhost:3000/` to view the application

## Running the sample in Spin

* Download Spin from https://github.com/fermyon/spin/releases and put it on your PATH
* Change to this directory
* Build the three projects by running `dotnet build`
* Run `spin up`
* Visit `http://localhost:3000/` to view the application
