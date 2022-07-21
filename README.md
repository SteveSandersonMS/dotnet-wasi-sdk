# Experimental WASI SDK for .NET Core

`Wasi.Sdk` is an experimental package that can build .NET Core projects (including whole ASP.NET Core applications) into standalone WASI-compliant `.wasm` files. These can then be run in standard WASI environments or custom WASI-like hosts.

## How to use: Console applications

```
dotnet new console -o MyFirstWasiApp
cd MyFirstWasiApp
dotnet add package Wasi.Sdk --prerelease
dotnet build
```

You'll see from the build output that this produces `bin/Debug/net7.0/MyFirstWasiApp.wasm`.

To run it,

 * Ensure you've installed [wasmtime](https://github.com/bytecodealliance/wasmtime) and it's available on your system `PATH`
 * Run your app via `dotnet run` or, if you're using Visual Studio, press Ctrl+F5

Alternatively you can invoke runners like `wasmtime` or `wasmer` manually on the command line. For example,

 * For [wasmtime](https://github.com/bytecodealliance/wasmtime), run `wasmtime bin/Debug/net7.0/MyFirstWasiApp.wasm`
 * For [wasmer](https://wasmer.io/), run `wasmer bin/Debug/net7.0/MyFirstWasiApp.wasm`

Other WASI hosts work similarly.

## How to use: ASP.NET Core applications

```
dotnet new web -o MyWebApp
cd MyWebApp
dotnet add package Wasi.Sdk --prerelease
dotnet add package Wasi.AspNetCore.Server.Native --prerelease
```

Then:

 * Open your new project in an IDE such as Visual Studio or VS Code
 * Open `Program.cs` and change the line `var builder = WebApplication.CreateBuilder(args)` to look like this:

   ```cs
   var builder = WebApplication.CreateBuilder(args).UseWasiConnectionListener();
   ```

 * Open `Properties/launchSettings.json` and edit the `applicationUrl` value to contain only a single HTTP listener, e.g.,

   ```json
   "applicationUrl": "http://localhost:8080"
   ```

 * Open your `.csproj` file (e.g., in VS, double-click on the project name) and, inside a `<PropertyGroup>`, add this:

   ```xml
   <WasiRunnerArgs>--tcplisten localhost:8080 --env ASPNETCORE_URLS=http://localhost:8080</WasiRunnerArgs>
   ```

   Instead of `8080`, you should enter the port number found in `Properties\launchSettings.json`.

That's it! You can now run it via `dotnet run` (or in VS, use Ctrl+F5)

Optionally, to add support for bundling `wwwroot` files into the `.wasm` file and serving them:

 * Add the NuGet package `Wasi.AspNetCore.BundledFiles`
 * In `Program.cs`, replace `app.UseStaticFiles();` with `app.UseBundledStaticFiles();`
 * In your `.csproj` file, add:

   ```xml
   <ItemGroup>
       <WasmBundleFiles Include="wwwroot\**" />
   </ItemGroup>
   ```

## What's in this repo

 * `Wasi.Sdk` - a package that causes your build to produce a WASI-compliant `.wasm` file. This works by:
   * Downloading the WASI SDK, if you don't already have it
   * When your regular .NET build is done, it takes the resulting assemblies, plus the .NET runtime precompiled to WebAssembly, and uses WASI SDK to bundle them into a single `.wasm` file. You can optionally include other native sources such as `.c` files in the compilation.
 * `Wasi.AspNetCore.BundledFiles` - provides `UseBundledStaticFiles`, and alternative to `UseStaticFiles`, that serves static files bundled into your `.wasm` file. This allows you to have single-file deployment even if you have files under `wwwroot` or elsewhere.
 * `Wasi.AspNetCore.Server.Native` - a way of running ASP.NET Core on WASI's TCP-level standard networking APIs (e.g., `sock_accept`). These standards are quite recent and are currently only supported in Wasmtime, not other WASI hosts.

... and more

## Building this repo from source

First, build the runtime. This can take quite a long time.

* `git submodule update --init --recursive`
* Do the following steps using Linux or WSL:
  * `sudo apt-get install build-essential cmake ninja-build python python3 zlib1g-dev`
* `cd modules/runtime/src/mono/wasm`
  * `make provision-wasm` (takes about 2 minutes)
  * `make build-all` (takes 10-15 minutes)
    * If you get an error about `setlocale: LC_ALL: cannot change locale` then  run `sudo apt install language-pack-en`. This only happens on very bare-bones machines.
* `cd ../wasi`
  * `make` (takes a few minutes - there are lots of warnings like "System is unknown to cmake" and that's OK)
  

Now you can build the packages and samples in this repo:

* Prerequisites
  * .NET 7 (`dotnet --version` should return `7.0.100-preview.4` or later)
  * Rust and the `wasm32-unknown-unknown` target (technically this is only needed for the CustomHost package)
    * [Install Rust](https://www.rust-lang.org/tools/install)
    * `rustup target add wasm32-unknown-unknown`
* Just use `dotnet build` or `dotnet run` on any of the samples or `src` projects, or open the solution in VS and Ctrl+F5 on any of the sample projects
