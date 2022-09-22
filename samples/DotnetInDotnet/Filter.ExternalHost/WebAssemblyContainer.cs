#pragma warning disable CS8605
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Wasmtime;

namespace HostingApplication;

public class WebAssemblyContainer
{
    private readonly Engine _engine;
    private readonly Module _module;
    private readonly Linker _linker;
    private readonly Store _store;
    private readonly Instance _instance;
    private readonly Function _malloc;
    private readonly Function _loadDll;
    private readonly Function _installFilter;
    private readonly Function _apply;
    private readonly Memory _memory;
    private static int jsonBufferPtr;
    private byte[] payloadMemory;

    // API doc https://bytecodealliance.github.io/wasmtime-dotnet/api/Wasmtime.Engine.html
    public WebAssemblyContainer(string webAssemblyFileName, string customFilterDllName, string asseblyQualifiedFilterName, int maxPayloadSizeBytes)
    {
        if (!File.Exists(webAssemblyFileName)) throw new FileNotFoundException(webAssemblyFileName);
        if (!File.Exists(customFilterDllName)) throw new FileNotFoundException(customFilterDllName);
        // RUST_BACKTRACE=1
        try
        {
            var config = new Config()
                .WithReferenceTypes(false)
                .WithWasmThreads(false)
                .WithCompilerStrategy(CompilerStrategy.Cranelift)
                .WithMaximumStackSize(10_000_000)
                .WithOptimizationLevel(OptimizationLevel.Speed)
            ;
            _engine = new Engine(config);
            _module = Module.FromFile(_engine, webAssemblyFileName);
            _linker = new Linker(_engine);
            _linker.DefineWasi();

            _store = new Store(_engine);
            var c = new WasiConfiguration()
                .WithInheritedStandardOutput()
                //.WithEnvironmentVariable("MONO_LOG_LEVEL", "debug")
                .WithEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1")
                ;
            _store.SetWasiConfiguration(c);

            _instance = _linker.Instantiate(_store, _module);
            _memory = _instance.GetMemory("memory")!;
            if (_memory == null) throw new InvalidOperationException("memory");

            _malloc = _instance.GetFunction("malloc")!;
            if (_malloc == null) throw new InvalidOperationException("malloc");
            _loadDll = _instance.GetFunction("loadDll")!;
            if (_loadDll == null) throw new InvalidOperationException("loadDll");
            _apply = _instance.GetFunction("apply")!;
            if (_apply == null) throw new InvalidOperationException("apply");
            _installFilter = _instance.GetFunction("installFilter")!;
            if (_installFilter == null) throw new InvalidOperationException("installFilter");

            payloadMemory = new byte[maxPayloadSizeBytes];

            LoadDll(customFilterDllName);
            Start();
            LoadFilter(asseblyQualifiedFilterName, maxPayloadSizeBytes);
        }
        catch (Exception ex)
        {
            _engine?.Dispose();
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    private unsafe void LoadDll(string customFilterDllName)
    {
        var shortDllName = Path.GetFileName(customFilterDllName);
        var dllNameBuffer = (int)_malloc.Invoke(shortDllName.Length * 2);
        if (dllNameBuffer == 0) throw new InvalidOperationException();
        var bytes = _memory.WriteString(dllNameBuffer, shortDllName, Encoding.UTF8);
        _memory.WriteByte(dllNameBuffer + bytes, 0);// zero terminated

        byte[] dllBytes = File.ReadAllBytes(customFilterDllName);
        var dllBytesBuffer = (int)_malloc.Invoke(dllBytes.Length);
        if (dllBytesBuffer == 0) throw new InvalidOperationException();

        var dllBufferSpan = _memory.GetSpan<byte>(dllBytesBuffer);
        var dllSource = new Span<byte>(dllBytes);
        dllSource.CopyTo(dllBufferSpan);

        _loadDll.Invoke(dllNameBuffer, dllBytesBuffer, dllBytes.Length);
    }

    private unsafe void Start()
    {
        var _start = _instance.GetFunction("_start")!;
        if (_start == null) throw new InvalidOperationException("_start");
        _start.Invoke();
    }

    private unsafe void LoadFilter(string asseblyQualifiedFilterName, int maxPayloadSizeBytes)
    {
        var filterNameBuffer = (int)_malloc.Invoke(asseblyQualifiedFilterName.Length * 2);
        if (filterNameBuffer == 0) throw new InvalidOperationException();
        var bytes = _memory.WriteString(filterNameBuffer, asseblyQualifiedFilterName, Encoding.UTF8);
        _memory.WriteByte(filterNameBuffer + bytes, 0);// zero terminated

        jsonBufferPtr = ((int)_installFilter.Invoke(filterNameBuffer, bytes, maxPayloadSizeBytes)!);
    }

    private bool ApplyFilter(ReadOnlyMemory<byte> eventUtf8Json)
    {
        var jsonBufferSpan = _memory.GetSpan<byte>(jsonBufferPtr);
        eventUtf8Json.Span.CopyTo(jsonBufferSpan);
        var filterResult = ((int)_apply.Invoke(eventUtf8Json.Length)!);
        return filterResult == 1;
    }

    public bool ApplyFilter(string json)
    {
        // the dotnet inside of the WASI host is not ready for threads
        lock (_engine)
        {
            var bytes = Encoding.UTF8.GetBytes(json, payloadMemory.AsSpan());
            return ApplyFilter(new ReadOnlyMemory<byte>(payloadMemory, 0, bytes));
        }
    }
}