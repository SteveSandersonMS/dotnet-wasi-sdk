#pragma warning disable CS8602
#pragma warning disable CS8604

using HostingApplication;

public class MainLoop
{
    public static void Main()
    {
        var container = CreateContainer("Filter.Custom", "Filter.Custom.Customer1UntrustedFilter");

        var pubSubEvent = DocumentSample();
        const int repeat = 10_000;
        bool result = false;
        var start = DateTime.UtcNow;
        for (var i = 0; i < repeat; i++)
        {
            result = container.ApplyFilter(pubSubEvent);
        }
        var duration = DateTime.UtcNow - start;

        Console.WriteLine($"filter repeated {repeat} times returned {result} in {duration.TotalMilliseconds}ms ");
    }

    public static string DocumentSample()
    {
        return @"{ 'productContext': 'FooBar' }".Replace('\'', '\"');
    }

    public static WebAssemblyContainer CreateContainer(string filterAssemblyName, string filterClassName)
    {
        const int maxPayloadSizeBytes = 100_000;
        string fullPath = System.Reflection.Assembly.GetAssembly(typeof(WebAssemblyContainer)).Location;
        string webAssemblyFileName = Path.Combine(Path.GetDirectoryName(fullPath), "Filter.WasmHost.wasm");
        string customFilterDllName = Path.Combine(Path.GetDirectoryName(fullPath), filterAssemblyName + ".dll");
        string asseblyQualifiedFilterName = filterClassName + ", " + filterAssemblyName;
        return new WebAssemblyContainer(webAssemblyFileName, customFilterDllName, asseblyQualifiedFilterName, maxPayloadSizeBytes);
    }
}