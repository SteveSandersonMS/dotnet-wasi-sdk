using System.Runtime.InteropServices;

//Console.WriteLine($"Hello, world at {DateTime.Now.ToLongTimeString()} on {RuntimeInformation.OSArchitecture}!");

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine($"Hello from C# at {DateTime.Now.ToLongTimeString()}");
        return 123;
    }

    static void Preinitialize()
    {
        // Unclear why, but unless we call this during preinitialization, we can't call it from Main
        // (gives "wasm trap: indirect call type mismatch"). Need to work out why.
        DateTime.Now.ToLongTimeString();
    }
}
