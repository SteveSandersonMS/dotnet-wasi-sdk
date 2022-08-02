using System.Globalization;
using System.Runtime.InteropServices;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine($"Hello, world at {DateTime.Now.ToLongTimeString()} on {RuntimeInformation.OSArchitecture}!");
        return 123;
    }

    static void Preinitialize()
    {
        // Unclear why, but unless we access something about localization during preinitialization,
        // anything that uses localization will fail at runtime
        _ = CultureInfo.CurrentCulture.Name;
    }
}
